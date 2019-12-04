using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Amazon.S3;
using Amazon.S3.Model;

using MosaicStepFunctions.Common;
using CloudMosaic.Common;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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
            var logger = new MosaicLogger(state, context);

            if (Directory.Exists(this.TileImageCacheDirectory))
            {
                Directory.Delete(this.TileImageCacheDirectory, true);
            }

            Directory.CreateDirectory(this.TileImageCacheDirectory);

            await logger.WriteMessageAsync("Loading mosaic layout info", MosaicLogger.Target.CloudWatchLogs);
            var mosaicLayoutInfo =
                await MosaicLayoutInfoManager.Load(this.S3Client, state.Bucket, state.MosaicLayoutInfoKey);

            var width = mosaicLayoutInfo.ColorMap.GetLength(0) * state.TileSize;
            var height = mosaicLayoutInfo.ColorMap.GetLength(1) * state.TileSize;

            await logger.WriteMessageAsync($"Creating pixel data array {width}x{height}", MosaicLogger.Target.CloudWatchLogs);
            var pixalData = new Rgba32[width * height];
            for (int i = 0; i < pixalData.Length; i++)
                pixalData[i] = Rgba32.Black;

            await logger.WriteMessageAsync($"Creating blank image", MosaicLogger.Target.CloudWatchLogs);
            using (var rawImage = Image.LoadPixelData(pixalData, width, height))
            {
                await logger.WriteMessageAsync($"Created blank image", MosaicLogger.Target.CloudWatchLogs);
                for (int x = 0; x < mosaicLayoutInfo.ColorMap.GetLength(0); x++)
                {
                    int xoffset = x * state.TileSize;
                    await logger.WriteMessageAsync($"Rendering row {x + 1} of {mosaicLayoutInfo.ColorMap.GetLength(0)}", MosaicLogger.Target.Client);
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
                    await logger.WriteMessageAsync("Saving rendered mosaic", MosaicLogger.Target.Client);
                    await logger.WriteMessageAsync(
                        $"Saving full mosaic to {destinationKey} with size {finalOutputStream.Length}", MosaicLogger.Target.CloudWatchLogs);

                    await this.S3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = state.Bucket,
                        Key = destinationKey,
                        InputStream = finalOutputStream
                    });
                }


                // Write web size mosaic to S3
                var webDimension = DetermineResizeDimension(rawImage.Width, rawImage.Height, Constants.IMAGE_WEB_WIDTH, Constants.IMAGE_WEB_HEIGHT);
                await logger.WriteMessageAsync($"Creating web page size version with dimensions width: {webDimension.width}, height: {webDimension.height}", MosaicLogger.Target.All);
                await SaveResize(state, rawImage, webDimension.width, webDimension.height,
                    S3KeyManager.DetermineS3Key(state.UserId, state.MosaicId, S3KeyManager.ImageType.WebMosaic),
                    context);

                // Write thumbnail mosaic to S3
                var thumbnailDimension = DetermineResizeDimension(rawImage.Width, rawImage.Height, Constants.IMAGE_THUMBNAIL_WIDTH, Constants.IMAGE_THUMBNAIL_HEIGHT);
                await logger.WriteMessageAsync($"Creating thumbnail version with dimensions width: {thumbnailDimension.width}, height: {thumbnailDimension.height}", MosaicLogger.Target.All);
                await SaveResize(state, rawImage, thumbnailDimension.width, thumbnailDimension.height,
                    S3KeyManager.DetermineS3Key(state.UserId, state.MosaicId, S3KeyManager.ImageType.ThumbnailMosaic),
                    context);

                state.Success = true;
                return state;
            }
        }

        (int width, int height) DetermineResizeDimension(int actualWidth, int actualHeight, int maxWidth, int maxHeight)
        {
            if (actualHeight < actualWidth)
            {
                return (maxWidth, (int)((double)actualHeight * (double)maxWidth / (double)actualWidth));
            }

            return ((int)((double)actualWidth * (double)maxHeight / (double)actualHeight), maxHeight);
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
