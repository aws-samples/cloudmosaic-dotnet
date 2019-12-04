using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Amazon.Lambda.Core;
using CloudMosaic.Communication.Manager;
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
            var logger = new MosaicLogger(state, context);

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
                    {"Status", new AttributeValueUpdate{Action= AttributeAction.PUT, Value = new AttributeValue{N = state.Success ? "1" : "2" } } }
                }
            };


            string errorMessage;
            if(!state.Success && (errorMessage = state.GetErrorMessage()) != null)
            {
                updateItemRequest.AttributeUpdates["ErrorMessage"] = new AttributeValueUpdate { Action = AttributeAction.PUT, Value = new AttributeValue { S = errorMessage } };
                await logger.WriteMessageAsync(new MessageEvent { Message = $"Mosaic render failed: {errorMessage}", CompleteEvent = false }, MosaicLogger.Target.All);
            }
            else
            {
                await logger.WriteMessageAsync(new MessageEvent { Message = "Mosaic render complete", CompleteEvent = true }, MosaicLogger.Target.All);
            }
            await this.DDBClient.UpdateItemAsync(updateItemRequest);

            return state;
        }
    }
}
