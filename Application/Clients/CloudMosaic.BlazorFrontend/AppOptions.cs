using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CloudMosaic.BlazorFrontend
{
    public class AppOptions
    {
        public string CloudMosaicApiUrl { get; set; }

        public string CloudMosaicWebSocketAPI { get; set; }

        public string MosaicStorageBucket { get; set; }

        public string UploadBucketPrefix { get; set; }
    }
}
