using System;

namespace CloudMosaic.API.Models
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
            this.ErrorMessage = mosaic.ErrorMessage;
        }

        public string MosaicId { get; set; }

        public string Name { get; set; }

        public DateTime CreateDate { get; set; }

        public Mosaic.MosaicStatuses Status { get; set; }

        public string ErrorMessage { get; set; }

        public string MosaicFullUrl { get; set; }
        public string MosaicThumbnailUrl { get; set; }
        public string MosaicWebPageSizeUrl { get; set; }
    }
}
