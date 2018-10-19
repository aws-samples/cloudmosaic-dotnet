using ImageMagick;
using System;
using System.Collections.Generic;
using System.Text;



namespace MosaicStepFunctions.Common
{
    public class MosaicLayoutInfo
    {
        public MosaicLayoutInfo()
        {
        }

        public string Key { get; set; }

        public MagickColor[,] ColorMap { get; set; }
        public Dictionary<int, string> IdToTileKey { get; set; }
        public int[,] TileMap { get; set; }




    }
}
