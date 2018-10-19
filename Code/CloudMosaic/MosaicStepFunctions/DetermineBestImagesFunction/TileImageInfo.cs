using System;
using System.Collections.Generic;
using System.Text;

using SixLabors.ImageSharp.PixelFormats;

namespace DetermineBestImagesFunction
{
    public class TileImageInfo
    {
        public string TileKey { get; set; }
        public Rgba32 AverageTL { get; set; }
        public Rgba32 AverageTR { get; set; }
        public Rgba32 AverageBL { get; set; }
        public Rgba32 AverageBR { get; set; }
    }
}
