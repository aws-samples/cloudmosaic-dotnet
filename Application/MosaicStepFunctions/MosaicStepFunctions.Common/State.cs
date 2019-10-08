namespace MosaicStepFunctions.Common
{
    public class State : CloudMosaic.Common.ExecutionInput
    {
        public const long SMALL_IMAGE_SIZE = 480000; // 800 * 600;
        public const long MEDIUM_IMAGE_SIZE = 1920000; // 1600 * 1200;
        public const long MAX_IMAGE_SIZE = 20000000; // 5000 * 4000;       
        
        public string MosaicLayoutInfoKey { get; set; }
        
        public long OriginalImagePixelCount { get; set; }
    }
}
