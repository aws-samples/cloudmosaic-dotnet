using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.ECS;
using Amazon.ECS.Model;

using KeyValuePair = Amazon.ECS.Model.KeyValuePair;

namespace CloudMosaic.Frontend
{
    public class ImportJobManager
    {
        AppOptions _appOptions;
        IAmazonECS _ecsClient;

        public ImportJobManager(IOptions<AppOptions> appOptions, IAmazonECS ecsClient)
        {
            this._appOptions = appOptions.Value;
            this._ecsClient = ecsClient;
        }

        public async System.Threading.Tasks.Task StartImport(string userId, string galleryId, string importUrl)
        {
            var runRequest = new RunTaskRequest
            {
                Cluster = this._appOptions.ECSCluster,
                TaskDefinition = this._appOptions.ECSTaskDefinition,
                Overrides = new TaskOverride
                {
                    ContainerOverrides = new List<ContainerOverride>
                    {
                        new ContainerOverride
                        {
                            Name = this._appOptions.ECSContainerDefinition,
                            Environment = new List<KeyValuePair>
                            {
                                new KeyValuePair{Name = Constants.ZIP_EXPANDER_BUCKET, Value = this._appOptions.ImageBucket},
                                new KeyValuePair{Name = Constants.ZIP_EXPANDER_DDB_TABLE, Value = this._appOptions.TableGallery},
                                new KeyValuePair{Name = Constants.ZIP_EXPANDER_USER_ID, Value = userId},
                                new KeyValuePair{Name = Constants.ZIP_EXPANDER_GALLERY_ID, Value = galleryId},
                                new KeyValuePair{Name = Constants.ZIP_EXPANDER_IMPORT_URL, Value = importUrl}
                            }
                        }
                    }
                },

                LaunchType = LaunchType.FARGATE,
                NetworkConfiguration = new NetworkConfiguration
                {
                    AwsvpcConfiguration = new AwsVpcConfiguration
                    {
                        SecurityGroups = new List<string> { this._appOptions.FargateSecurityGroup },
                        Subnets = new List<string> { this._appOptions.FargateSubnet },
                        AssignPublicIp = AssignPublicIp.ENABLED
                    }
                }                
            };

            await this._ecsClient.RunTaskAsync(runRequest);
        }
    }
}
