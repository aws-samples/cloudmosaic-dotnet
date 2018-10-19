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

using ImageMagick;

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
            this.TileImageCacheDirectory = "/tmp/tiles"; 
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            if(Directory.Exists(this.TileImageCacheDirectory))
            {
                Directory.Delete(this.TileImageCacheDirectory, true);
            }

            Directory.CreateDirectory(this.TileImageCacheDirectory);

            context.Logger.LogLine("Loading mosaic layout info");
            var mosaicLayoutInfo = await MosaicLayoutInfoManager.Load(this.S3Client, state.Bucket, state.MosaicLayoutInfoKey);

            var tileSize = 50;
            var width = 300 * tileSize;
            var height = 200 * tileSize;

            context.Logger.LogLine($"Creating canvas image to hold tiles {width}x{height}");
            using (var rawImage = new MagickImage(MagickColor.FromRgb(0, 0, 0), width, height))
            {
                var rawPixels = rawImage.GetPixelsUnsafe();

                for (int x = 0; x < mosaicLayoutInfo.ColorMap.GetLength(0); x++)
                {
                    context.Logger.LogLine($"Processing row {x}");
                    for (int y = 0; y < mosaicLayoutInfo.ColorMap.GetLength(1); y++)
                    {
                        var tileId = mosaicLayoutInfo.TileMap[x, y];
                        var tileKey = mosaicLayoutInfo.IdToTileKey[tileId];

                        using (var tileImage = await LoadTile(state.Bucket, tileKey, context))
                        {
                            var tileArea = tileImage.GetPixelsUnsafe().GetArea(0, 0, tileSize, tileSize);
                            rawPixels.SetArea(x, y, tileSize, tileSize, tileArea);
                        }
                    }
                }

                var finalOutputStream = new MemoryStream();
                rawImage.Write(finalOutputStream, MagickFormat.Png24);
                finalOutputStream.Position = 0;

                context.Logger.LogLine($"Saving mosaic to {state.DestinationKey} with size {finalOutputStream.Length}");
                await this.S3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = state.Bucket,
                    Key = state.DestinationKey,
                    InputStream = finalOutputStream
                });
            }

            
            

            return state;
        }

        private async Task<MagickImage> LoadTile(string bucket, string tileKey, ILambdaContext context)
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

            return new MagickImage(localPath);
        }
    }
}
