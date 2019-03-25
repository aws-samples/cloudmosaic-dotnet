using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.Json;


using Amazon.S3;
using Amazon.S3.Model;

namespace CustomRuntimeExample
{
    public class Function
    {


        /// <summary>
        /// The main entry point for the custom runtime.
        /// </summary>
        /// <param name="args"></param>
        private static async Task Main(string[] args)
        {
            Func<string, ILambdaContext, Task<string>> func = FunctionHandlerAsync;
            using(var handlerWrapper = HandlerWrapper.GetHandlerWrapper(func, new JsonSerializer()))
            using(var bootstrap = new LambdaBootstrap(handlerWrapper))
            {
                await bootstrap.RunAsync();
            }
        }

        static IAmazonS3 _s3Client = new AmazonS3Client();

        public static async Task<string> FunctionHandlerAsync(string bucketName, ILambdaContext context)
        {
            var objects = new List<string>();

            // Use new async enumerable
            await foreach (var response in GetS3ListResponsesAsync(bucketName))
            {
                response.S3Objects.ForEach(x => objects.Add(x.Key));
            }

            // Use new Index features
            var secondToLastObject = objects.ToArray()[^2];

            return secondToLastObject;
        }

        public static async IAsyncEnumerable<ListObjectsV2Response> GetS3ListResponsesAsync(string bucketName)
        {
            ListObjectsV2Request request = new ListObjectsV2Request() {BucketName = bucketName };
            ListObjectsV2Response response = null;
            do
            {
                request.ContinuationToken = response?.ContinuationToken;

                response = await _s3Client.ListObjectsV2Async(request);
                yield return response;

            } while (response.IsTruncated);
        }
    }
}
