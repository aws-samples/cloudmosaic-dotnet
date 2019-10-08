namespace CloudMosaic.Common
{
    public class ExecutionInput
    {
        public int PixelBlock { get; set; } = 10;
        public int TileSize { get; set; } = 50;

        public string UserId { get; set; }
        public string MosaicId { get; set; }
        public string GalleryId { get; set; }
        public string Bucket { get; set; }
        public string TableGalleryItems { get; set; }
        public string TableMosaic { get; set; }
        public string SourceKey { get; set; }
    }
}
