using System;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

using Amazon.S3;
using Amazon.Lambda.Core;

using MosaicStepFunctions.Common;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
            var tmpPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(state.SourceKey));
            try
            {
                context.Logger.LogLine("Saving image to tmp");
                using (var response = await S3Client.GetObjectAsync(state.Bucket, state.SourceKey))
                {
                    await response.WriteResponseStreamToFileAsync(tmpPath, false, default(CancellationToken));
                }

                var mosaicLayoutInfo = MosaicLayoutInfoManager.Create();
                state.MosaicLayoutInfoKey = mosaicLayoutInfo.Key;

                context.Logger.LogLine($"Loading image {tmpPath}. File size {new FileInfo(tmpPath).Length}");
                using (var sourceImage = Image.Load(tmpPath))
                {
                    state.OriginalImagePixelCount = sourceImage.Width * sourceImage.Height;
                    if (state.OriginalImagePixelCount < State.SMALL_IMAGE_SIZE)
                    {
                        state.PixelBlock = 3;
                    }
                    else if (state.OriginalImagePixelCount < State.MEDIUM_IMAGE_SIZE)
                    {
                        state.PixelBlock = 5;
                    }
                    else if(state.OriginalImagePixelCount < State.MAX_IMAGE_SIZE)
                    {
                        state.PixelBlock = 10;
                    }
                    else
                    {
                        throw new Exception("Image too large to make a mosaic");
                    }
                
                    mosaicLayoutInfo.ColorMap = CreateMap(state, sourceImage);
                }

                context.Logger.LogLine($"Color map created: {mosaicLayoutInfo.ColorMap.GetLength(0)}x{mosaicLayoutInfo.ColorMap.GetLength(1)}");

                await MosaicLayoutInfoManager.Save(S3Client, state.Bucket, mosaicLayoutInfo);
                context.Logger.LogLine($"Saving mosaic layout info to {mosaicLayoutInfo.Key}");

                return state;
            }
            finally 
            {
                if (File.Exists(tmpPath))
                {
                    File.Delete(tmpPath);
                }
            }
        }

        public Rgba32[,] CreateMap(State state, Image<Rgba32> image)
        {
            int horizontalTiles = (int)image.Width / state.PixelBlock;
            int verticalTiles = (int)image.Height / state.PixelBlock;

            var colorMap = new Rgba32[horizontalTiles, verticalTiles];

            int tileWidth = (image.Width - image.Width % horizontalTiles) / horizontalTiles;
            int tileHeight = (image.Height - image.Height % verticalTiles) / verticalTiles;

            Int64 r, g, b;
            int pixelCount;

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
                            var c = image[xPos, yPos];
                            r += c.R;
                            g += c.G;
                            b += c.B;
                            pixelCount++;
                        }
                    }
                    colorMap[x, y] = new Rgba32((byte)(r / pixelCount), (byte)(g / pixelCount), (byte)(b / pixelCount));
                }

            }
            return colorMap;
        }
    }
}
