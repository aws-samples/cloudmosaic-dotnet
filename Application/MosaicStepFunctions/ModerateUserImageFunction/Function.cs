using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;

using Amazon.Rekognition;
using Amazon.Rekognition.Model;

using MosaicStepFunctions.Common;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace ModerateUserImageFunction
{
    public class Function
    {
        IAmazonRekognition RekognitionClient { get; set; }

        public Function()
        {
            this.RekognitionClient = new AmazonRekognitionClient();
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            var logger = new MosaicLogger(state, context);

            await logger.WriteMessageAsync("Checking to make sure image is demo safe", MosaicLogger.Target.Client);
            var response = await RekognitionClient.DetectModerationLabelsAsync(new DetectModerationLabelsRequest
            {
                Image = new Image
                {
                    S3Object = new S3Object
                    {
                        Bucket = state.Bucket,
                        Name = state.SourceKey
                    }
                }
            });

            if (response.ModerationLabels.Count > 0)
            {
                throw new Exception("Image deemed not demo safe");
            }

            await logger.WriteMessageAsync("Image past demo safe check", MosaicLogger.Target.Client);

            return state;
        }
    }
}
