using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace GalleryItemStreamProcessor
{
    public class Function
    {
        const string TILE_COUNT_ATTRIBUTE = "TileCount";
        const string USER_ATTRIBUTE = "UserId";
        const string GALLERY_ATTRIBUTE = "GalleryId";

        IAmazonDynamoDB DynamoDBClient { get; set; }

        string _tableGallery = "Gallery";

        public Function()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LAMBDA_TASK_ROOT")))
            {
                Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();
            }

            DynamoDBClient = new AmazonDynamoDBClient();

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TABLE_GALLERY")))
            {
                this._tableGallery = Environment.GetEnvironmentVariable("TABLE_GALLERY");
            }

            Console.WriteLine($"Gallery table configured to {this._tableGallery}");
        }


        public async Task FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
        {
            context.Logger.LogLine($"Beginning to process {dynamoEvent.Records.Count} records...");

            // Determine for each gallery in the collection of records how much each should be updated.
            // This way instead of calling DynamoDB for each item in the records we only need to call it once
            // per unique gallery id.
            var batchIncrements = new Dictionary<(string UserId, string GalleryId), long>();
            foreach (var record in dynamoEvent.Records)
            {
                var galleryId = record.Dynamodb.Keys["GalleryId"].S;
                var tileKey = record.Dynamodb.Keys["TileKey"].S;
                var userId = tileKey.Split('/')[2];

                var batchKey = (UserId: userId, GalleryId: galleryId);
                if(!batchIncrements.ContainsKey(batchKey))
                {
                    batchIncrements[batchKey] = 0;
                }


                if(record.EventName == OperationType.INSERT)
                {
                    batchIncrements[batchKey]++;
                }
                else if(record.EventName == OperationType.REMOVE)
                {
                    batchIncrements[batchKey]--;
                }
                else
                {
                    continue;
                }
            }


            foreach(var kvp in batchIncrements)
            {
                try
                {
                    // It would be 0 if there was only a modification to a gallery item
                    if (kvp.Value == 0)
                        continue;

                    var request = new UpdateItemRequest
                    {
                        TableName = _tableGallery,
                        Key = new Dictionary<string, AttributeValue>
                        {
                            {"UserId", new AttributeValue{S = kvp.Key.UserId } },
                            {"GalleryId", new AttributeValue{S = kvp.Key.GalleryId } }
                        },
                        UpdateExpression = "ADD #tc :increment",

                        // Add conditional expression to make sure the update doesn't create a partial item if 
                        // if the item was previously deleted. 
                        ConditionExpression = "#u = :u and #g = :g",
                        ExpressionAttributeNames = new Dictionary<string, string>
                        {
                            { "#tc", TILE_COUNT_ATTRIBUTE },
                            { "#u", USER_ATTRIBUTE },
                            { "#g", GALLERY_ATTRIBUTE },
                        },
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            {":increment", new AttributeValue{N = kvp.Value.ToString()}},
                            {":u", new AttributeValue{S = kvp.Key.UserId}},
                            {":g", new AttributeValue{S = kvp.Key.GalleryId}}
                        },
                        ReturnValues = ReturnValue.UPDATED_NEW
                    };

                    var response = await DynamoDBClient.UpdateItemAsync(request);
                    context.Logger.LogLine($"Updated gallery {kvp.Key.GalleryId} from user {kvp.Key.UserId} by increment {kvp.Value} to count {response.Attributes[TILE_COUNT_ATTRIBUTE].N}");
                }
                catch (Exception e)
                {
                    context.Logger.LogLine($"Failed to update {kvp.Key.GalleryId} from user {kvp.Key.UserId} by increment {kvp.Value}: {e.Message}");
                }
            }

            context.Logger.LogLine("Stream processing complete.");
        }

    }
}