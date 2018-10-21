using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CloudMosaic.Frontend.Models
{
    public class MosaicSummary
    {
        public MosaicSummary()
        {

        }

        public MosaicSummary(Mosaic mosaic)
        {
            this.MosaicId = mosaic.MosaicId;
            this.Name = mosaic.Name;
            this.CreateDate = mosaic.CreateDate;
            this.Status = mosaic.Status;
        }

        public string MosaicId { get; set; }

        public string Name { get; set; }

        public DateTime CreateDate { get; set; }

        public Mosaic.Statuses Status { get; set; }


        public string MosaicFullUrl { get; set; }
        public string MosaicThumbnailUrl { get; set; }
    }
}
