using System;

using Amazon.DynamoDBv2.DataModel;

namespace CloudMosaic.API.Models
{
    public class Gallery
    {
        public enum GalleryStatuses { Importing = 0, Ready = 1, Failed = 2 }

        public enum GallerySharingState { Private = 0, Public = 1 }

        public string UserId { get; set; }

        public string GalleryId { get; set; }

        public string Name { get; set; }

        public string Decription { get; set; }

        public long TileCount { get; set; }

        public GalleryStatuses Status { get; set; }

        [DynamoDBProperty(Converter = typeof(EnumPropertyConverter))]
        public GallerySharingState Sharing { get; set; }
        
        public DateTime CreateDate { get; set; }
    }
}
