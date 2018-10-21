using System;
using System.Collections.Generic;
using System.Text;

namespace CloudMosaic.Common
{
    public static class S3KeyManager
    {
        public enum ImageType { Original, FullMosaic, WebMosaic, ThumbnailMosaic }

        public static string DetermineS3Key(string userId, string mosaicId, ImageType type)
        {
            return $"Mosaic/{userId}/{mosaicId}/{type.ToString()}.jpg";
        }
    }
}
