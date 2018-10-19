using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using MosaicStepFunctions.Common;

using Amazon.S3;
using Amazon.S3.Model;

using ImageMagick;
using System.IO;
using System.Threading;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]


namespace CreateColorMapFunction
{
    public class Function
    {
        IAmazonS3 S3Client { get; set; }
        
        public Function()
        {
            this.S3Client = new AmazonS3Client();
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            var tmpPath = Path.Combine("/tmp/", Path.GetFileName(state.SourceKey));
            context.Logger.LogLine("Saving image to tmp");
            using (var response = await S3Client.GetObjectAsync(state.Bucket, state.SourceKey))
            {
                await response.WriteResponseStreamToFileAsync(tmpPath, false, default(CancellationToken));
            }

            var mosaicLayoutInfo = MosaicLayoutInfoManager.Create();
            state.MosaicLayoutInfoKey = mosaicLayoutInfo.Key;

            context.Logger.LogLine($"Loading image {tmpPath}. File size {new FileInfo(tmpPath).Length}");
            using (var sourceImage = new MagickImage(tmpPath))
            {
                mosaicLayoutInfo.ColorMap = CreateMap(sourceImage);
            }

            context.Logger.LogLine($"Color mape created: {mosaicLayoutInfo.ColorMap.GetLength(0)}x{mosaicLayoutInfo.ColorMap.GetLength(1)}");

            await MosaicLayoutInfoManager.Save(S3Client, state.Bucket, mosaicLayoutInfo);
            context.Logger.LogLine($"Saving mosaic layout info to {mosaicLayoutInfo.Key}");

            return state;
        }

        public MagickColor[,] CreateMap(MagickImage image)
        {
            int horizontalTiles = (int)image.Width / 20;
            int verticalTiles = (int)image.Height / 20;

            var colorMap = new MagickColor[horizontalTiles, verticalTiles];

            int tileWidth = (image.Width - image.Width % horizontalTiles) / horizontalTiles;
            int tileHeight = (image.Height - image.Height % verticalTiles) / verticalTiles;

            Int64 r, g, b;
            int pixelCount;
            var pixels = image.GetPixelsUnsafe();

            int xPos, yPos;

            for (int x = 0; x < horizontalTiles; x++)
            {
                for (int y = 0; y < verticalTiles; y++)
                {
                    r = 0;
                    g = 0;
                    b = 0;
                    pixelCount = 0;

                    for (xPos = tileWidth * x; xPos < x * tileWidth + tileWidth; xPos++)
                    {
                        for (yPos = tileHeight * y; yPos < y * tileHeight + tileHeight; yPos++)
                        {
                            var c = pixels[xPos, yPos].ToColor();
                            r += c.R;
                            g += c.G;
                            b += c.B;
                            pixelCount++;
                        }
                    }
                    colorMap[x, y] = MagickColor.FromRgb((byte)(r / pixelCount), (byte)(g / pixelCount), (byte)(b / pixelCount));
                }

            }
            return colorMap;
        }
    }
}
