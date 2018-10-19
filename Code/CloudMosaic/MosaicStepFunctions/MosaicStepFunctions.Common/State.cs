using System;
using System.Collections.Generic;


namespace MosaicStepFunctions.Common
{
    public class State
    {
        public int PixelBlock { get; set; } = 10;
        public int TileSize { get; set; } = 50;

        public string GalleryId { get; set; }
        public string Bucket { get; set; }
        public string SourceKey { get; set; }
        public string DestinationKey { get; set; }

        public string MosaicLayoutInfoKey { get; set; }


    }
}
