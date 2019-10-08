using System;
using System.IO;
using System.Threading.Tasks;

using Amazon.S3;
using Amazon.S3.Model;

namespace MosaicStepFunctions.Common
{
    public static class MosaicLayoutInfoManager
    {
        public static MosaicLayoutInfo Create()
        {
            return new MosaicLayoutInfo() { Key = "Cache/" + Guid.NewGuid().ToString() + ".json" };
        }

        public static Task Save(IAmazonS3 s3Client, string bucket, MosaicLayoutInfo info)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(info);
            return s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = info.Key,
                ContentBody = json
            });
        }

        public static async Task<MosaicLayoutInfo> Load(IAmazonS3 s3Client, string bucket, string key)
        {
            using (var response = await s3Client.GetObjectAsync(bucket, key))
            using (var reader = new StreamReader(response.ResponseStream))
            {
                var json = await reader.ReadToEndAsync();
                var info = Newtonsoft.Json.JsonConvert.DeserializeObject<MosaicLayoutInfo>(json);
                return info;
            }
        }
    }
}
