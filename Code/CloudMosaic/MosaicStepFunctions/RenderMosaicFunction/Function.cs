using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using MosaicStepFunctions.Common;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Amazon.S3;
using Amazon.S3.Model;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using CloudMosaic.Common;
using SixLabors.ImageSharp.Processing;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace RenderMosaicFunction
{
    public class Function
    {
        public IAmazonS3 S3Client { get; set; }
        string TileImageCacheDirectory { get; set; }

        public Function()
        {
            this.S3Client = new AmazonS3Client();
            this.TileImageCacheDirectory = Path.Combine(Path.GetTempPath(), "tiles"); 
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            if (Directory.Exists(this.TileImageCacheDirectory))
            {
                Directory.Delete(this.TileImageCacheDirectory, true);
            }

            Directory.CreateDirectory(this.TileImageCacheDirectory);

            context.Logger.LogLine("Loading mosaic layout info");
            var mosaicLayoutInfo =
                await MosaicLayoutInfoManager.Load(this.S3Client, state.Bucket, state.MosaicLayoutInfoKey);

            var width = mosaicLayoutInfo.ColorMap.GetLength(0) * state.TileSize;
            var height = mosaicLayoutInfo.ColorMap.GetLength(1) * state.TileSize;

            context.Logger.LogLine($"Creating pixel data array {width}x{height}");
            var pixalData = new Rgba32[width * height];
            for (int i = 0; i < pixalData.Length; i++)
                pixalData[i] = Rgba32.Black;

            context.Logger.LogLine($"Creating blank image");
            using (var rawImage = Image.LoadPixelData(pixalData, width, height))
            {
                context.Logger.LogLine($"Created blank image");
                for (int x = 0; x < mosaicLayoutInfo.ColorMap.GetLength(0); x++)
                {
                    int xoffset = x * state.TileSize;
                    context.Logger.LogLine($"Processing row {x}");
                    for (int y = 0; y < mosaicLayoutInfo.ColorMap.GetLength(1); y++)
                    {
                        int yoffset = y * state.TileSize;
                        var tileId = mosaicLayoutInfo.TileMap[x, y];
                        var tileKey = mosaicLayoutInfo.IdToTileKey[tileId];

                        using (var tileImage = await LoadTile(state.Bucket, tileKey, context))
                        {
                            for (int x1 = 0; x1 < state.TileSize; x1++)
                            {
                                for (int y1 = 0; y1 < state.TileSize; y1++)
                                {
                                    rawImage[x1 + xoffset, y1 + yoffset] = tileImage[x1, y1];
                                }
                            }
                        }
                    }
                }

                // Write full mosaic to S3
                {
                    var finalOutputStream = new MemoryStream();
                    rawImage.Save(finalOutputStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
                    finalOutputStream.Position = 0;

                    var destinationKey =
                        S3KeyManager.DetermineS3Key(state.UserId, state.MosaicId, S3KeyManager.ImageType.FullMosaic);
                    context.Logger.LogLine(
                        $"Saving full mosaic to {destinationKey} with size {finalOutputStream.Length}");
                    await this.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = state.Bucket,
                        Key = destinationKey,
                        InputStream = finalOutputStream
                    });
                }

                // Write web size mosaic to S3
                await SaveResize(state, rawImage, Constants.IMAGE_WEB_WIDTH, Constants.IMAGE_WEB_HEIGHT,
                    S3KeyManager.DetermineS3Key(state.UserId, state.MosaicId, S3KeyManager.ImageType.WebMosaic),
                    context);

                // Write thumbnail mosaic to S3
                await SaveResize(state, rawImage, Constants.IMAGE_THUMBNAIL_WIDTH, Constants.IMAGE_THUMBNAIL_HEIGHT,
                    S3KeyManager.DetermineS3Key(state.UserId, state.MosaicId, S3KeyManager.ImageType.ThumbnailMosaic),
                    context);

                return state;
            }
        }

        private async Task SaveResize(State state, Image<Rgba32> image, int width, int height, string key, ILambdaContext context)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new SixLabors.Primitives.Size { Width = width, Height = height },
                Mode = ResizeMode.Stretch
            }));

            var stream = new MemoryStream();
            image.Save(stream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
            stream.Position = 0;

            context.Logger.LogLine($"Saving web mosaic to {key} with size {stream.Length}");
            await this.S3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = state.Bucket,
                Key = key,
                InputStream = stream
            });
            
        }

        private async Task<Image<Rgba32>> LoadTile(string bucket, string tileKey, ILambdaContext context)
        {
            var localPath = Path.Combine(this.TileImageCacheDirectory, Path.GetFileName(tileKey));
            if(!File.Exists(localPath))
            {
                using (var response = await this.S3Client.GetObjectAsync(bucket, tileKey))
                {
                    context.Logger.LogLine($"... Downloading tile {tileKey}");
                    await response.WriteResponseStreamToFileAsync(localPath, false, default(CancellationToken));
                }
            }

            return Image.Load(localPath);
        }
    }
}
