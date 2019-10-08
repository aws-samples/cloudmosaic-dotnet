using SixLabors.ImageSharp.PixelFormats;

namespace ProcessRawImage
{
    public class ImageInfo
    {
        public Rgba32 AverageTL { get; set; }
        public Rgba32 AverageTR { get; set; }
        public Rgba32 AverageBL { get; set; }
        public Rgba32 AverageBR { get; set; }
    }
}
