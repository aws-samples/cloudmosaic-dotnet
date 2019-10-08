namespace CloudMosaic.Common
{
    public static class S3KeyManager
    {
        public enum ImageType { Original, FullMosaic, WebMosaic, ThumbnailMosaic, TileGallerySource }

        public static string DetermineS3Key(string userId, string mosaicOrGalleryId, ImageType type)
        {
            if (type == ImageType.TileGallerySource)
            {
                return $"TileGallerySource/{userId}/{mosaicOrGalleryId}/{type.ToString()}.zip";
            }

            return $"Mosaic/{userId}/{mosaicOrGalleryId}/{type.ToString()}.jpg";
        }
    }
}
