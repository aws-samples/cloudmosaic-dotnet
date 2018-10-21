using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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


        public string ImageBucket { get; set; }


        public string StateMachineArn { get; set; }        
        
    }
}
