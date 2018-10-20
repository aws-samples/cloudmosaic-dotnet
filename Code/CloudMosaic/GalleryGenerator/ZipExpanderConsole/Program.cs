using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

using Amazon.Rekognition;
using Amazon.Rekognition.Model;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Amazon.S3;
using Amazon.S3.Model;

using ImageMagick;

namespace ZipExpanderConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var config = GetConfig(args);
            var extractDirectoryRoot = Path.Combine(config.Temp, DateTime.Now.Ticks.ToString());
            var extractDirectoryImages = Path.Combine(extractDirectoryRoot, "images");
            var downloadZipPath = Path.Combine(extractDirectoryRoot, "processing.zip");
            if (!Directory.Exists(extractDirectoryRoot))
            {
                Directory.CreateDirectory(extractDirectoryRoot);
                Directory.CreateDirectory(extractDirectoryImages);
            }

            using (var s3Client = new AmazonS3Client())
            using (var rekClient = new AmazonRekognitionClient())
            using (var ddbClient = new AmazonDynamoDBClient())
            {

                int lastPercentReported = 0;
                long totalRead = 0;
                Console.WriteLine($"Downloading zip archive from: {config.ImportUrl}");
                using (var httpClient = new HttpClient())
                using (var message = await httpClient.GetAsync(config.ImportUrl))
                using (var stream = await message.Content.ReadAsStreamAsync())
                using (var localStream = File.OpenWrite(downloadZipPath))
                {
                    var buffer = new byte[8192];
                    int readLength;
                    while ((readLength = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await localStream.WriteAsync(buffer, 0, readLength);

                        totalRead += readLength;

                        var percent = (int)(totalRead / (double)message.Content.Headers.ContentLength * 100.0);
                        if (lastPercentReported != percent)
                        {
                            lastPercentReported = percent;
                            Console.WriteLine($"... {percent}%");
                        }
                    }

                    Console.WriteLine($"Download complete to {downloadZipPath}, file size {new FileInfo(downloadZipPath).Length}");
                }

                using (var localStream = File.OpenRead(downloadZipPath))
                using (var archive = new ZipArchive(localStream))
                {
                    int processed = 0;
                    var entries = archive.Entries;
                    foreach (var entry in archive.Entries)
                    {
                        try
                        {
                            Console.WriteLine($"{processed}/{entries.Count} Processing {Path.GetFileName(entry.FullName)}");
                            var content = new MemoryStream();
                            using (var zipStream = entry.Open())
                            {
                                zipStream.CopyTo(content);
                            }
                            content.Position = 0;

                            var destinationKey = $"Galleries/Raw/{config.UserId}/{config.GalleryId}/{Path.GetFileName(entry.FullName)}";
                            await s3Client.PutObjectAsync(new PutObjectRequest
                            {
                                BucketName = config.Bucket,
                                Key = destinationKey,
                                InputStream = content
                            });


                            Console.WriteLine("Image passed moderation test");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Error processing entry: " + e.Message);
                            Console.WriteLine(e.StackTrace);
                        }

                        processed++;
                    }
                }

                var updateItemRequest = new UpdateItemRequest
                {
                    TableName = config.DDBTable,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "UserId", new AttributeValue{S = config.UserId } },
                        { "GalleryId", new AttributeValue{S = config.GalleryId } }
                    },
                    AttributeUpdates = new Dictionary<string, AttributeValueUpdate>
                    {
                        {"Status", new AttributeValueUpdate{Action= AttributeAction.PUT, Value = new AttributeValue{N = "1" } } }
                    }
                };

                await ddbClient.UpdateItemAsync(updateItemRequest);

                Console.WriteLine("Import Complete");
            }

            Directory.Delete(extractDirectoryRoot, true);
        }


        static Config GetConfig(string[] args)
        {
            const string ZIP_EXPANDER_IMPORT_URL = "ZIP_EXPANDER_IMPORT_URL";
            const string ZIP_EXPANDER_DDB_TABLE = "ZIP_EXPANDER_DDB_TABLE";
            const string ZIP_EXPANDER_BUCKET = "ZIP_EXPANDER_BUCKET";
            const string ZIP_EXPANDER_USER_ID = "ZIP_EXPANDER_USER_ID";
            const string ZIP_EXPANDER_GALLERY_ID = "ZIP_EXPANDER_GALLERY_ID";
            const string ZIP_EXPANDER_TEMP = "ZIP_EXPANDER_TEMP";

            var config = new Config();


            if (args.Length > 0)
            {
                config.ImportUrl = args[0];
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ZIP_EXPANDER_IMPORT_URL)))
            {
                config.ImportUrl = Environment.GetEnvironmentVariable(ZIP_EXPANDER_IMPORT_URL);
            }
            else
            {
                throw new Exception($"Missing required parameter {ZIP_EXPANDER_IMPORT_URL}");
            }

            if (args.Length > 1)
            {
                config.DDBTable = args[1];
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ZIP_EXPANDER_DDB_TABLE)))
            {
                config.DDBTable = Environment.GetEnvironmentVariable(ZIP_EXPANDER_DDB_TABLE);
            }
            else
            {
                throw new Exception($"Missing required parameter {ZIP_EXPANDER_DDB_TABLE}");
            }

            if (args.Length > 2)
            {
                config.Bucket = args[2];
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ZIP_EXPANDER_BUCKET)))
            {
                config.Bucket = Environment.GetEnvironmentVariable(ZIP_EXPANDER_BUCKET);
            }
            else
            {
                throw new Exception($"Missing required parameter {ZIP_EXPANDER_BUCKET}");
            }


            if (args.Length > 3)
            {
                config.UserId = args[3];
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ZIP_EXPANDER_USER_ID)))
            {
                config.UserId = Environment.GetEnvironmentVariable(ZIP_EXPANDER_USER_ID);
            }
            else
            {
                throw new Exception($"Missing required parameter {ZIP_EXPANDER_USER_ID}");
            }

            if (args.Length > 4)
            {
                config.GalleryId = args[4];
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ZIP_EXPANDER_GALLERY_ID)))
            {
                config.GalleryId = Environment.GetEnvironmentVariable(ZIP_EXPANDER_GALLERY_ID);
            }
            else
            {
                throw new Exception($"Missing required parameter {ZIP_EXPANDER_GALLERY_ID}");
            }

            if (args.Length > 5)
            {
                config.Temp = args[5];
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ZIP_EXPANDER_TEMP)))
            {
                config.Temp = Environment.GetEnvironmentVariable(ZIP_EXPANDER_TEMP);
            }
            else
            {
                config.Temp = Path.GetTempPath();
            }

            return config;
        }

        public class Config
        {
            public string ImportUrl { get; set; }
            public string DDBTable { get; set; }
            public string Bucket { get; set; }
            public string UserId { get; set; }
            public string GalleryId { get; set; }
            public string Temp { get; set; }
        }
    }
}
