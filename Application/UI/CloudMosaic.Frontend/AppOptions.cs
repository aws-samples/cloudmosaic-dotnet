namespace CloudMosaic.Frontend
{
    public class AppOptions
    {
        public string ECSCluster { get; set; }
        public string ECSTaskDefinition { get; set; }
        public string ECSContainerDefinition { get; set; }
        public string FargateSecurityGroup { get; set; }
        public string FargateSubnet { get; set; }


        public string TableGallery { get; set; }
        public string TableGalleryItems { get; set; }
        public string TableMosaic { get; set; }

        public string JobQueueArn { get; set; }
        public string JobDefinitionArn { get; set; }

        public string MosaicStorageBucket { get; set; }

        public string StateMachineArn { get; set; }        
    }
}
