using System;

namespace CloudMosaic.Frontend.Models
{
    public class Mosaic
    {
        public enum Statuses {Creating=0, Ready=1}

        public string UserId { get; set; }

        public string MosaicId { get; set; }

        public string Name { get; set; }

        public DateTime CreateDate { get; set; }

        public Statuses Status { get; set; }

        public string ExecutionArn { get; set; }
    }
}