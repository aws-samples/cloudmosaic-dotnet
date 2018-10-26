using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;

using Amazon.Rekognition;
using Amazon.Rekognition.Model;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Amazon.S3;
using Amazon.S3.Model;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using System.Text;
using System.Threading;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ProcessRawImage
{
    public class Function
    {
        const string QUALITY_ENV = "Quality";
        const string TILE_SIZE_ENV = "TileSize";
        int TileSize { get; set; } = 50;
        int Quality { get; set; } = 6;

        IAmazonS3 S3Client { get; set; }

        IAmazonRekognition RekognitionClient { get; set; }

        IAmazonDynamoDB DynamoDBClient { get; set; }

        string _tableGalleryItems = "GalleryItems";

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();

            S3Client = new AmazonS3Client();
            RekognitionClient = new AmazonRekognitionClient();
            DynamoDBClient = new AmazonDynamoDBClient();

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(TILE_SIZE_ENV)))
            {
                TileSize = int.Parse(Environment.GetEnvironmentVariable(TILE_SIZE_ENV));
            }
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(QUALITY_ENV)))
            {
                Quality = int.Parse(Environment.GetEnvironmentVariable(QUALITY_ENV));
            }

            if(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TableGalleryItems")))
            {
                this._tableGalleryItems = Environment.GetEnvironmentVariable("TableGalleryItems");                
            }

            Console.WriteLine($"Gallery Item table configured to {this._tableGalleryItems}");
        }

        /// <summary>
        /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client"></param>
        public Function(IAmazonS3 s3Client)
        {
            this.S3Client = s3Client;
        }
        
        public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            context.Logger.LogLine($"Received event with {evnt.Records.Count} records");
            foreach(var record in evnt.Records)
            {
                var bucket = record.S3.Bucket.Name;
                var originalKey =  System.Net.WebUtility.UrlDecode(record.S3.Object.Key);
                context.Logger.LogLine($"Processing s3://{bucket}/{originalKey}");

                var tokens = originalKey.Split('/');
                var galleryId = tokens[tokens.Length - 2];
                context.Logger.LogLine($"GalleryId: {galleryId}");

                if(!(await IsImageSafe(bucket, originalKey, context)))
                {
                    context.Logger.LogLine("Image suspected to be inappropriate and skipped.");
                    continue;
                }

                context.Logger.LogLine("Image passed moderation test");

                var tmpPath = Path.Combine("/tmp/", Path.GetFileName(originalKey));
                try
                {
                    context.Logger.LogLine("Saving image to tmp");
                    using (var response = await S3Client.GetObjectAsync(bucket, originalKey))
                    {
                        await response.WriteResponseStreamToFileAsync(tmpPath, false, default(CancellationToken));
                    }
                    context.Logger.LogLine("Reading image");
                    using (var sourceImage = Image.Load(tmpPath))
                    {
                        var imageInfo = GetAverageColor(sourceImage, context);
                        context.Logger.LogLine($"Width: {sourceImage.Width}, Height: {sourceImage.Height} TL: {imageInfo.AverageTL}, TR: {imageInfo.AverageTR}, BL: {imageInfo.AverageBL}, BR: {imageInfo.AverageBR}");
                        var tileKey = await UploadTile(sourceImage, bucket, originalKey, context);

                        await SaveToTable(galleryId, tileKey, imageInfo);
                    }
                }
                finally
                {
                    File.Delete(tmpPath);
                }
            }
        }

        private async Task SaveToTable(string galleryId, string tileKey, ImageInfo imageInfo)
        {
            var putRequest = new PutItemRequest
            {
                TableName = this._tableGalleryItems,
                Item = new Dictionary<string, AttributeValue>
                        {
                            {"GalleryId", new AttributeValue{S = galleryId } },
                            {"TileKey", new AttributeValue {S = tileKey} },
                            {"TL", new AttributeValue{M = new Dictionary<string, AttributeValue>
                                    {
                                        { "R", new AttributeValue {N = imageInfo.AverageTL.R.ToString() } },
                                        { "G", new AttributeValue {N = imageInfo.AverageTL.G.ToString() } },
                                        { "B", new AttributeValue {N = imageInfo.AverageTL.B.ToString() } }
                                    } }
                            },
                            {"TR", new AttributeValue{M = new Dictionary<string, AttributeValue>
                                    {
                                        { "R", new AttributeValue {N = imageInfo.AverageTR.R.ToString() } },
                                        { "G", new AttributeValue {N = imageInfo.AverageTR.G.ToString() } },
                                        { "B", new AttributeValue {N = imageInfo.AverageTR.B.ToString() } }
                                    } }
                            },
                            {"BL", new AttributeValue{M = new Dictionary<string, AttributeValue>
                                    {
                                        { "R", new AttributeValue {N = imageInfo.AverageBL.R.ToString() } },
                                        { "G", new AttributeValue {N = imageInfo.AverageBL.G.ToString() } },
                                        { "B", new AttributeValue {N = imageInfo.AverageBL.B.ToString() } }
                                    } }
                            },
                            {"BR", new AttributeValue{M = new Dictionary<string, AttributeValue>
                                    {
                                        { "R", new AttributeValue {N = imageInfo.AverageBR.R.ToString() } },
                                        { "G", new AttributeValue {N = imageInfo.AverageBR.G.ToString() } },
                                        { "B", new AttributeValue {N = imageInfo.AverageBR.B.ToString() } }
                                    } }
                            }
                        }
            };

            await DynamoDBClient.PutItemAsync(putRequest);
        }

        public ImageInfo GetAverageColor(Image<Rgba32> image, ILambdaContext context)
        {
            var imageInfo = new ImageInfo();

            int halfX = image.Width / 2;
            int halfY = image.Height / 2;

            imageInfo.AverageTL = GetAverageColor(image, 0, 0, halfX, halfY, context);
            imageInfo.AverageTR = GetAverageColor(image, halfX, 0, image.Width, halfY, context);
            imageInfo.AverageBL = GetAverageColor(image, 0, halfY, halfX, image.Height, context);
            imageInfo.AverageBR = GetAverageColor(image, halfX, halfY, image.Width, image.Height, context);

            return imageInfo;
        }

        private Rgba32 GetAverageColor(Image<Rgba32> image, int sx, int sy, int width, int height, ILambdaContext context)
        {
            Int64 r = 0, g = 0, b = 0;
            int p = 0;

            for (int x = sx; x < width; x += this.Quality)
            {
                for (int y = sy; y < height; y += this.Quality)
                {
                    var pixel = image[x, y];
                    r += pixel.R;
                    g += pixel.G;
                    b += pixel.B;
                    p++;
                }
            }

            return new Rgba32((byte)(r / p), (byte)(g / p), (byte)(b / p));
        }

        private async Task<string> UploadTile(Image<Rgba32> image, string bucket, string originalKey, ILambdaContext context)
        {
            var imageBuffer = new MemoryStream();

            var resizeOptions = new ResizeOptions
            {
                Size = new SixLabors.Primitives.Size { Width = this.TileSize, Height = this.TileSize},
                Mode = ResizeMode.Stretch
            };
            image.Mutate(x => x.Resize(resizeOptions));
            image.Save(imageBuffer, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());

            imageBuffer.Position = 0;

            var tileImageKey = originalKey.Replace("Raw", "Tiles");
            int pos = tileImageKey.LastIndexOf('.');
            tileImageKey = tileImageKey.Substring(0, pos) + ".jpg";

            await S3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = tileImageKey,
                InputStream = imageBuffer
            });
            context.Logger.LogLine($"Tile uploaded to {tileImageKey}");
            return tileImageKey;
        }



        private async Task<bool> IsImageSafe(string bucket, string key, ILambdaContext context)
        {
            var response = await RekognitionClient.DetectModerationLabelsAsync(new DetectModerationLabelsRequest
            {
                Image = new Amazon.Rekognition.Model.Image
                {
                    S3Object = new Amazon.Rekognition.Model.S3Object
                    {
                        Bucket = bucket,
                        Name = key
                    }
                }
            });

            if(response.ModerationLabels.Count > 0)
            {
                var sb = new StringBuilder();
                foreach(var label in response.ModerationLabels)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");
                    if(!string.IsNullOrEmpty(label.ParentName))
                    {
                        sb.Append(label.ParentName + "/");
                    }

                    sb.Append($"{label.Name}:{label.Confidence}");
                }

                context.Logger.LogLine($"The following moderation labels were found: {sb.ToString()}");
            }
            return response.ModerationLabels.Count == 0;
        }
    }
}
