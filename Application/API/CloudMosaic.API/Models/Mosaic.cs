using System;

namespace CloudMosaic.API.Models
{
    public class Mosaic
    {
        public enum MosaicStatuses {Creating=0, Completed=1, Failed=2}

        public string ErrorMessage { get; set; }

        public string UserId { get; set; }

        public string MosaicId { get; set; }

        public string Name { get; set; }

        public DateTime CreateDate { get; set; }

        public MosaicStatuses Status { get; set; }

        public string ExecutionArn { get; set; }
    }
}