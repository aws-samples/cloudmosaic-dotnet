using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Amazon.Lambda.Core;

using MosaicStepFunctions.Common;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace NotifyCompletionFunction
{
    public class Function
    {
        public IAmazonDynamoDB DDBClient { get; set; }

        public Function()
        {
            this.DDBClient = new AmazonDynamoDBClient();
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            var updateItemRequest = new UpdateItemRequest
            {
                TableName = state.TableMosaic,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "UserId", new AttributeValue{S = state.UserId } },
                    { "MosaicId", new AttributeValue{S = state.MosaicId } }
                },
                AttributeUpdates = new Dictionary<string, AttributeValueUpdate>
                {
                    {"Status", new AttributeValueUpdate{Action= AttributeAction.PUT, Value = new AttributeValue{N = "1" } } }
                }
            };

            await this.DDBClient.UpdateItemAsync(updateItemRequest);

            // TODO Send Email if email address exist

            return state;
        }
    }
}
