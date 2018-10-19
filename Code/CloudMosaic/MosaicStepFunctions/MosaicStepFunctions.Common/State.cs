using System;
using System.Collections.Generic;

using ImageMagick;

namespace MosaicStepFunctions.Common
{
    public class State
    {
        public string GalleryId { get; set; }
        public string Bucket { get; set; }
        public string SourceKey { get; set; }
        public string DestinationKey { get; set; }

        public string MosaicLayoutInfoKey { get; set; }


    }
}
