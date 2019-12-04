using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Amazon.Batch;
using Amazon.Batch.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

using CloudMosaic.Common;

using CloudMosaic.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;

namespace CloudMosaic.API.Controllers
{
    /// <summary>
    /// API to manage tile galleries for an authenticated user.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class GalleryController : Controller
    {
        AppOptions _appOptions;
        IAmazonDynamoDB _ddbClient;
        DynamoDBContext _ddbContext;
        IAmazonS3 _s3Client;
        IAmazonBatch _batchClient;

        /// <summary>
        /// Constructor for the controller.
        /// </summary>
        /// <param name="appOptions"></param>
        /// <param name="s3Client"></param>
        /// <param name="ddbClient"></param>
        /// <param name="batchClient"></param>
        public GalleryController(IOptions<AppOptions> appOptions, IAmazonS3 s3Client, IAmazonDynamoDB ddbClient, IAmazonBatch batchClient)
        {
            this._appOptions = appOptions.Value;

            this._ddbClient = ddbClient;
            this._s3Client = s3Client;
            this._batchClient = batchClient;

            this._ddbContext = new DynamoDBContext(this._ddbClient);
        }

        /// <summary>
        /// Get the list of galleries the user has created.
        /// </summary>
        /// <param name="includePublic">If true then also include the galleries that have been marked as public.</param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(200, Type = typeof(Gallery[]))]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<JsonResult> GetUserGalleries([FromQuery]bool? includePublic)
        {
            var userId = Utilities.GetUsername(this.HttpContext.User);
            var search = this._ddbContext.QueryAsync<Gallery>(userId);

            var galleries = await search.GetRemainingAsync().ConfigureAwait(false);

            if(includePublic.GetValueOrDefault())
            {
                var scanConfig = new DynamoDBOperationConfig
                {
                    IndexName = "SharingState"
                };
                var publicSearch = this._ddbContext.ScanAsync<Gallery>(new ScanCondition[]
                    { new ScanCondition("Sharing", Amazon.DynamoDBv2.DocumentModel.ScanOperator.Equal, Gallery.GallerySharingState.Public)},
                    scanConfig);

                var publicGalleries = await publicSearch.GetRemainingAsync().ConfigureAwait(false);

                foreach(var publicGallery in publicGalleries)
                {
                    if(!galleries.Exists(x => string.Equals(x.GalleryId, publicGallery.GalleryId, StringComparison.OrdinalIgnoreCase)))
                    {
                        galleries.Add(publicGallery);
                    }
                }
            }

            return new JsonResult(galleries);
        }

        /// <summary>
        /// Create a new empty gallery.
        /// </summary>
        /// <param name="name">The name of the gallery.</param>
        /// <returns>The gallery id to use for adding tiles to the gallery.</returns>
        [HttpPut("{name}")]
        [ProducesResponseType(200, Type = typeof(CreateGalleryResult))]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Admin")]
        public async Task<JsonResult> CreateGallery(string name)
        {
            var userId = Utilities.GetUsername(this.HttpContext.User);
            var gallery = new Gallery
            {
                UserId = userId,
                GalleryId = $"{name}-{Guid.NewGuid().ToString()}",
                Name = name,
                CreateDate = DateTime.UtcNow,
                Status = Gallery.GalleryStatuses.Ready
            };

            await this._ddbContext.SaveAsync(gallery).ConfigureAwait(false);

            return new JsonResult(new CreateGalleryResult { GalleryId = gallery.GalleryId });
        }

        class CreateGalleryResult
        {
            public string GalleryId { get; set; }
        }

        /// <summary>
        /// Delete a gallery.
        /// </summary>
        /// <param name="galleryId">The id of the gallery to delete.</param>
        /// <returns></returns>
        [HttpDelete("{galleryId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Admin")]
        public async Task<IActionResult> DeleteGallery(string galleryId)
        {
            var userId = Utilities.GetUsername(this.HttpContext.User);

            await this._ddbContext.DeleteAsync<Gallery>(userId, galleryId);
            return Ok();
        }

        /// <summary>
        /// Start an import job to add images from a zip file to a gallery.
        /// </summary>
        /// <param name="galleryId">Gallery to add images to.</param>
        /// <param name="sourceZipUrl">URL to a zip file of images that will be imported.</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Admin")]
        public async Task<IActionResult> SubmitGalleryImportJob([FromQuery] string galleryId, [FromQuery] string sourceZipUrl)
        {
            var userId = Utilities.GetUsername(this.HttpContext.User);

            var submitRequest = new SubmitJobRequest
            {
                JobQueue = this._appOptions.JobQueueArn,
                JobDefinition = this._appOptions.JobDefinitionArn,
                JobName = $"{Utilities.MakeSafeName(galleryId, 128)}",
                ContainerOverrides = new ContainerOverrides
                {
                    Environment = new List<Amazon.Batch.Model.KeyValuePair>
                    {
                        new Amazon.Batch.Model.KeyValuePair{Name = Constants.ZIP_EXPANDER_BUCKET, Value = this._appOptions.MosaicStorageBucket},
                        new Amazon.Batch.Model.KeyValuePair{Name = Constants.ZIP_EXPANDER_DDB_TABLE, Value = this._appOptions.TableGallery},
                        new Amazon.Batch.Model.KeyValuePair{Name = Constants.ZIP_EXPANDER_USER_ID, Value = userId},
                        new Amazon.Batch.Model.KeyValuePair{Name = Constants.ZIP_EXPANDER_GALLERY_ID, Value = galleryId},
                        new Amazon.Batch.Model.KeyValuePair{Name = Constants.ZIP_EXPANDER_IMPORT_URL, Value = sourceZipUrl}
                    }
                }
            };

            await this._batchClient.SubmitJobAsync(submitRequest).ConfigureAwait(false);


            var gallery = await this._ddbContext.LoadAsync<Gallery>(userId, galleryId);
            gallery.Status = Gallery.GalleryStatuses.Importing;
            await this._ddbContext.SaveAsync(gallery);

            return Ok();
        }
    }
}
