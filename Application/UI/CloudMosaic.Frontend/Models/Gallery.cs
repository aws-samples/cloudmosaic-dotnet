﻿using System;

namespace CloudMosaic.Frontend.Models
{
    public class Gallery
    {
        public enum Statuses { Importing = 0, Ready = 1 }

        public string UserId { get; set; }

        public string GalleryId { get; set; }

        public string Name { get; set; }

        public string Attributions { get; set; }

        public Statuses Status { get; set; }
        
        public bool IsPublic { get; set; }
        
        public DateTime CreateDate { get; set; }
    }
}
