using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MosaicStepFunctions.Common
{
    public class State : CloudMosaic.Common.ExecutionInput
    {
        public const long SMALL_IMAGE_SIZE = 480000; // 800 * 600;
        public const long MEDIUM_IMAGE_SIZE = 1920000; // 1600 * 1200;
        public const long MAX_IMAGE_SIZE = 6000000; // 3000 * 2000;       
        
        public string MosaicLayoutInfoKey { get; set; }
        
        public long OriginalImagePixelCount { get; set; }

        public long MosaicImagePixelCount { get; set; }

        public bool Success { get; set; }

        public ExceptionType Exception { get; set; }

        public string GetErrorMessage()
        {
            if (Exception?.Cause == null)
                return null;

            try
            {
                var jobj = JsonConvert.DeserializeObject(Exception.Cause) as JObject;
                return jobj["errorMessage"]?.ToString();
            }
            catch
            {
                return Exception.Cause;
            }
        }

        public class ExceptionType
        {
            public string Error { get; set; }

            public string Cause { get; set; }            
        }
    }
}
