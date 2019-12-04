using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;

namespace CloudMosaic.Communication.Manager
{
    public class MessageEvent
    {
        public enum ResourceTypes { Mosaic, Gallery};


        public MessageEvent()
        {

        }

        public MessageEvent(string targetUser, ResourceTypes type, string resourceId)
        {
            this.TargetUser = targetUser;
            this.ResourceType = type;
            this.ResourceId = resourceId;
        }


        public string TargetUser { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ResourceTypes ResourceType { get; set; }
        public string ResourceId { get; set; }
        public string Message { get; set; }

        public bool CompleteEvent { get; set; }
    }
}
