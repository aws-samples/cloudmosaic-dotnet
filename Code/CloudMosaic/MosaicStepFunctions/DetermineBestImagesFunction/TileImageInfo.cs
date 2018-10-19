using System;
using System.Collections.Generic;
using System.Text;

using ImageMagick;

namespace DetermineBestImagesFunction
{
    public class TileImageInfo
    {
        public string TileKey { get; set; }
        public MagickColor AverageTL { get; set; }
        public MagickColor AverageTR { get; set; }
        public MagickColor AverageBL { get; set; }
        public MagickColor AverageBR { get; set; }
    }
}
