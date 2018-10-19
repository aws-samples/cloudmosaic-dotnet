using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

using Amazon.Rekognition;
using Amazon.Rekognition.Model;

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
            {
                Console.WriteLine($"Bucket: {config.Bucket}, Key: {config.ZipArchiveKey}");

                int lastPercentReported = 0;
                long totalRead = 0;
                Console.WriteLine("Opening S3 Stream");
                using (var response = await s3Client.GetObjectAsync(config.Bucket, config.ZipArchiveKey))
                using (var responseStream = response.ResponseStream)
                using (var localStream = File.OpenWrite(downloadZipPath))
                {
                    var buffer = new byte[8192];
                    int readLength;
                    while ((readLength = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await localStream.WriteAsync(buffer, 0, readLength);

                        totalRead += readLength;

                        var percent = (int)(totalRead / (double)response.ContentLength * 100.0);
                        if (lastPercentReported != percent)
                        {
                            lastPercentReported = percent;
                            Console.WriteLine($"... {percent}%");
                        }
                    }
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

                            var destinationKey = $"Galleries/Raw/{config.GalleryId}/{Path.GetFileName(entry.FullName)}";
                            await s3Client.PutObjectAsync(new PutObjectRequest
                            {
                                BucketName = config.Bucket,
                                Key = destinationKey,
                                InputStream = content
                            });


                            Console.WriteLine("Image passed moderation test");
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine($"Error processing entry: " + e.Message);
                            Console.WriteLine(e.StackTrace);
                        }

                        processed++;
                    }
                }

                Console.WriteLine("Complete");
            }

            Directory.Delete(extractDirectoryRoot, true);
        }


        static Config GetConfig(string[] args)
        {
            const string BUCKET_ENV = "ZIP_EXPANDER_BUCKET";
            const string ZIP_ARCHIVE_KEY_ENV = "ZIP_EXPANDER_ZIP_ARCHIVE_KEY";
            const string GALLERY_ID_ENV = "ZIP_EXPANDER_GALLERY_ID";
            const string TEMP_ENV = "ZIP_EXPANDER_TEMP";

            var config = new Config();
            if(args.Length > 0)
            {
                config.Bucket = args[0];
            }
            else if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(BUCKET_ENV)))
            {
                config.Bucket = Environment.GetEnvironmentVariable(BUCKET_ENV);
            }
            else
            {
                throw new Exception("S3 bucket not configured.");
            }

            if (args.Length > 1)
            {
                config.ZipArchiveKey = args[1];
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(ZIP_ARCHIVE_KEY_ENV)))
            {
                config.ZipArchiveKey = Environment.GetEnvironmentVariable(ZIP_ARCHIVE_KEY_ENV);
            }
            else
            {
                throw new Exception("S3 zip archive key not configured.");
            }


            if (args.Length > 2)
            {
                config.GalleryId = args[2];
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(GALLERY_ID_ENV)))
            {
                config.GalleryId = Environment.GetEnvironmentVariable(GALLERY_ID_ENV);
            }
            else
            {
                config.GalleryId = Path.GetFileNameWithoutExtension(config.ZipArchiveKey);
            }

            if (args.Length > 3)
            {
                config.Temp = args[3];
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(TEMP_ENV)))
            {
                config.Temp = Environment.GetEnvironmentVariable(TEMP_ENV);
            }
            else
            {
                config.Temp = Path.GetTempPath();
            }

            return config;
        }

        public class Config
        {
            public string Bucket { get; set; }
            public string ZipArchiveKey { get; set; }
            public string Temp { get; set; }
            public string GalleryId { get; set; }
        }
    }
}
