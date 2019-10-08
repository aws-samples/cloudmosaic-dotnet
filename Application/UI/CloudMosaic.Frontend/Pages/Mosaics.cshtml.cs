using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

using CloudMosaic.Frontend.Models;
using CloudMosaic.Common;

using Microsoft.Extensions.Options;

namespace CloudMosaic.Frontend.Pages
{
    [Authorize]
    public class MosaicsModel : PageModel
    {
        DynamoDBContext _ddbContext;
        IAmazonS3 _s3Client;

        AppOptions _appOptions;
        public IList<MosaicSummary> Mosaics { get; set; }

        public MosaicsModel(IOptions<AppOptions> appOptions, IAmazonDynamoDB ddbClient, IAmazonS3 s3Client)
        {
            this._appOptions = appOptions.Value;
            this._ddbContext = new DynamoDBContext(ddbClient);
            this._s3Client = s3Client;
        }

        public async Task OnGet()
        {
            var search = this._ddbContext.QueryAsync<Mosaic>(this.HttpContext.User.Identity.Name);

            this.Mosaics = new List<MosaicSummary>();
            foreach (var item in (await search.GetRemainingAsync()).OrderByDescending(x => x.CreateDate))
            {
                var summary = new MosaicSummary(item)
                {
                    MosaicFullUrl = this._s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
                    {
                        BucketName = _appOptions.MosaicStorageBucket,
                        Key = S3KeyManager.DetermineS3Key(this.HttpContext.User.Identity.Name, item.MosaicId, S3KeyManager.ImageType.FullMosaic),
                        Expires = DateTime.UtcNow.AddHours(1)
                    }),
                    MosaicThumbnailUrl = this._s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
                    {
                        BucketName = _appOptions.MosaicStorageBucket,
                        Key = S3KeyManager.DetermineS3Key(this.HttpContext.User.Identity.Name, item.MosaicId, S3KeyManager.ImageType.ThumbnailMosaic),
                        Expires = DateTime.UtcNow.AddHours(1)
                    })
                };

                this.Mosaics.Add(summary);
            }
        }
    }
}