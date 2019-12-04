using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using CloudMosaic.Communication.Manager;

namespace MosaicStepFunctions.Common
{
    public class MosaicLogger
    {
        [Flags]
        public enum Target { CloudWatchLogs=1, Client=2, All=0xFFFFFFF};

        ILambdaContext _context;
        ILambdaLogger _lambdaLogger;
        State _state;
        CommunicationManager _manager;


        public MosaicLogger(State state, ILambdaContext context)
        {
            _context = context;
            _lambdaLogger = this._context?.Logger;
            _state = state;

            try
            {
                var connectionTable = Environment.GetEnvironmentVariable("COMMUNICATION_CONNECTION_TABLE");
                context.Logger.LogLine($"Configuring CommunicationManager to use connection table '{connectionTable}'");
                _manager = CommunicationManager.CreateManager(connectionTable);
            }
            catch(Exception e)
            {
                _lambdaLogger.LogLine($"Communication manager failed to initialize: {e.Message}");
            }
        }

        public async Task WriteMessageAsync(string message, Target visibiliy)
        {
            var evnt = new MessageEvent{ Message = message };
            await WriteMessageAsync(evnt, visibiliy);
        }

        public async Task WriteMessageAsync(MessageEvent evnt, Target visibiliy)
        {
            if((visibiliy & Target.CloudWatchLogs) == Target.CloudWatchLogs)
            {
                _lambdaLogger?.LogLine($"{this._context.AwsRequestId}: {evnt.Message}");
            }

            if (_manager != null && (visibiliy & Target.Client) == Target.Client)
            {
                evnt.TargetUser = this._state.UserId;
                evnt.ResourceType = MessageEvent.ResourceTypes.Mosaic;
                evnt.ResourceId = this._state.MosaicId;

                await _manager.SendMessage(evnt);
            }
        }

        public void WriteMessage(string message, Target visibiliy)
        {
            if ((visibiliy & Target.CloudWatchLogs) == Target.CloudWatchLogs)
            {
                _lambdaLogger?.LogLine($"{this._context.AwsRequestId}: {message}");
            }

            if (_manager != null && (visibiliy & Target.Client) == Target.Client)
            {
                var evnt = new MessageEvent(this._state.UserId, MessageEvent.ResourceTypes.Mosaic, this._state.MosaicId) { Message = message };
                _manager.SendMessage(evnt).GetAwaiter().GetResult();
            }
        }
    }
}
