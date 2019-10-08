using System.Collections.Generic;
using SixLabors.ImageSharp.PixelFormats;

namespace MosaicStepFunctions.Common
{
    public class MosaicLayoutInfo
    {
        public MosaicLayoutInfo()
        {
        }

        public string Key { get; set; }

        public Rgba32[,] ColorMap { get; set; }
        public Dictionary<int, string> IdToTileKey { get; set; }
        public int[,] TileMap { get; set; }
    }
}
