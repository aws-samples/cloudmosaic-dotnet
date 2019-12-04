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
            var logger = new MosaicLogger(state, context);
            var tmpPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(state.SourceKey));
            try
            {
                await logger.WriteMessageAsync("Fetching image", MosaicLogger.Target.All );
                using (var response = await S3Client.GetObjectAsync(state.Bucket, state.SourceKey))
                {
                    await response.WriteResponseStreamToFileAsync(tmpPath, false, default(CancellationToken));
                }

                var mosaicLayoutInfo = MosaicLayoutInfoManager.Create();
                state.MosaicLayoutInfoKey = mosaicLayoutInfo.Key;

                await logger.WriteMessageAsync($"Loading image {tmpPath}. File size {new FileInfo(tmpPath).Length}", MosaicLogger.Target.CloudWatchLogs);
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
                        state.PixelBlock = 10;
                        await logger.WriteMessageAsync("Image is too large so doing an initial resize of the source image.", MosaicLogger.Target.All);
                        ImageUtilites.ResizeImage(sourceImage, 3000, 2000);
                    }

                    // Compute how many pixels the mosaic image will be
                    state.MosaicImagePixelCount = state.OriginalImagePixelCount / (state.PixelBlock * state.PixelBlock) * (state.TileSize * state.TileSize);

                    await logger.WriteMessageAsync("Breaking image into a color map", MosaicLogger.Target.All);
                    mosaicLayoutInfo.ColorMap = CreateMap(state, sourceImage);
                }

                await logger.WriteMessageAsync($"Color map created of {mosaicLayoutInfo.ColorMap.GetLength(0)} rows and {mosaicLayoutInfo.ColorMap.GetLength(1)} columns", MosaicLogger.Target.Client);

                await MosaicLayoutInfoManager.Save(S3Client, state.Bucket, mosaicLayoutInfo);
                await logger.WriteMessageAsync($"Saving mosaic layout info to {mosaicLayoutInfo.Key}", MosaicLogger.Target.CloudWatchLogs);

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
