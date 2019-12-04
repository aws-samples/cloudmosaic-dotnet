using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;

using CloudMosaic.Common;

using CloudMosaic.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace CloudMosaic.API.Controllers
{
    /// <summary>
    /// API to manage mosaics for an authenticated user.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MosaicController : Controller
    {
        AppOptions _appOptions;
        IAmazonDynamoDB _ddbClient;
        DynamoDBContext _ddbContext;
        IAmazonS3 _s3Client;
        IAmazonStepFunctions _stepClient;

        /// <summary>
        /// Constructor for the controller.
        /// </summary>
        /// <param name="appOptions"></param>
        /// <param name="s3Client"></param>
        /// <param name="stepClient"></param>
        /// <param name="ddbClient"></param>
        public MosaicController(IOptions<AppOptions> appOptions, IAmazonS3 s3Client, IAmazonStepFunctions stepClient, IAmazonDynamoDB ddbClient)
        {
            this._appOptions = appOptions.Value;

            this._ddbClient = ddbClient;
            this._s3Client = s3Client;
            this._stepClient = stepClient;

            this._ddbContext = new DynamoDBContext(this._ddbClient);
        }

        /// <summary>
        /// Get the list of mosaics for a user. The mosaic will contain temporary URL to the actual mosaic images.
        /// </summary>
        /// <returns></returns>
        [HttpGet()]
        [ProducesResponseType(200, Type = typeof(MosaicSummary[]))]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<JsonResult> GetUserMosaic()
        {
            var userId = Utilities.GetUsername(this.HttpContext.User);

            var search = this._ddbContext.QueryAsync<Mosaic>(userId);

            var mosaics = new List<MosaicSummary>();
            foreach (var item in (await search.GetRemainingAsync()).OrderByDescending(x => x.CreateDate))
            {
                mosaics.Add(ConvertToMosaicSummary(item));
            }

            return new JsonResult(mosaics);
        }

        /// <summary>
        /// Gets a feed of all of the mosaics created. This API requires Admin access.
        /// </summary>
        /// <returns></returns>
        [HttpGet("feed")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "Admin")]
        [ProducesResponseType(200, Type = typeof(MosaicSummary[]))]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<JsonResult> GetAllMosaic()
        {
            var userId = Utilities.GetUsername(this.HttpContext.User);

            var search = this._ddbContext.ScanAsync<Mosaic>(new ScanCondition[] { new ScanCondition("Status", Amazon.DynamoDBv2.DocumentModel.ScanOperator.Equal, Mosaic.MosaicStatuses.Completed)});

            var mosaics = new List<MosaicSummary>();
            foreach (var item in (await search.GetRemainingAsync()).OrderByDescending(x => x.CreateDate))
            {
                mosaics.Add(ConvertToMosaicSummary(item));
            }

            return new JsonResult(mosaics);
        }

        /// <summary>
        /// Deletes a mosaic.
        /// </summary>
        /// <param name="mosaicId">The id of the mosaic to delete.</param>
        /// <returns></returns>
        [HttpDelete("{mosaicId}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> DeleteMosaic(string mosaicId)
        {
            var userId = Utilities.GetUsername(this.HttpContext.User);

            await this._ddbContext.DeleteAsync<Mosaic>(userId, mosaicId);
            return Ok();
        }

        /// <summary>
        /// Starts a job to create the mosaic.
        /// </summary>
        /// <param name="galleryId">The gallery id to use to create the mosaic.</param>
        /// <param name="name">The name of the mosaic to be created.</param>
        /// <param name="sourceImageUrl">The URL to the image to be converted into a mosaic.</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> SubmitMosaicJob([FromQuery] string galleryId, [FromQuery] string name, [FromQuery] string sourceImageUrl)
        {
            var userId = Utilities.GetUsername(this.HttpContext.User);
            var tempFile = Path.GetTempFileName();
            try
            {
                var mosaicId = name + "-" + Guid.NewGuid().ToString();
                using (var fileStream = System.IO.File.OpenWrite(tempFile))
                {
                    await Utilities.CopyStreamAsync(sourceImageUrl, fileStream);
                }

                var putRequest = new PutObjectRequest
                {
                    BucketName = this._appOptions.MosaicStorageBucket,
                    Key = S3KeyManager.DetermineS3Key(userId, mosaicId, S3KeyManager.ImageType.Original),
                    FilePath = tempFile
                };
                await this._s3Client.PutObjectAsync(putRequest).ConfigureAwait(false);

                var mosaic = new Mosaic
                {
                    UserId = userId,
                    MosaicId = mosaicId,
                    CreateDate = DateTime.UtcNow,
                    Name = name,
                    Status = Mosaic.MosaicStatuses.Creating
                };

                var input = new ExecutionInput
                {
                    TableGalleryItems = this._appOptions.TableGalleryItems,
                    TableMosaic = this._appOptions.TableMosaic,
                    Bucket = this._appOptions.MosaicStorageBucket,
                    SourceKey = putRequest.Key,
                    GalleryId = galleryId,
                    MosaicId = mosaicId,
                    UserId = userId
                };

                var stepResponse = await this._stepClient.StartExecutionAsync(new StartExecutionRequest
                {
                    StateMachineArn = this._appOptions.StateMachineArn,
                    Name = $"{Utilities.MakeSafeName(putRequest.Key, 80)}",
                    Input = JsonSerializer.Serialize(input)
                }).ConfigureAwait(false);

                mosaic.ExecutionArn = stepResponse.ExecutionArn;
                await this._ddbContext.SaveAsync(mosaic).ConfigureAwait(false);

                return Ok();
            }
            finally
            {
                if (System.IO.File.Exists(tempFile))
                {
                    System.IO.File.Delete(tempFile);
                }
            }
        }

        private MosaicSummary ConvertToMosaicSummary(Mosaic mosaic)
        {
            var summary = new MosaicSummary(mosaic)
            {
                MosaicFullUrl = this._s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
                {
                    BucketName = _appOptions.MosaicStorageBucket,
                    Key = S3KeyManager.DetermineS3Key(mosaic.UserId, mosaic.MosaicId, S3KeyManager.ImageType.FullMosaic),
                    Expires = DateTime.UtcNow.AddHours(1)
                }),
                MosaicThumbnailUrl = this._s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
                {
                    BucketName = _appOptions.MosaicStorageBucket,
                    Key = S3KeyManager.DetermineS3Key(mosaic.UserId, mosaic.MosaicId, S3KeyManager.ImageType.ThumbnailMosaic),
                    Expires = DateTime.UtcNow.AddHours(1)
                }),
                MosaicWebPageSizeUrl = this._s3Client.GetPreSignedURL(new GetPreSignedUrlRequest
                {
                    BucketName = _appOptions.MosaicStorageBucket,
                    Key = S3KeyManager.DetermineS3Key(mosaic.UserId, mosaic.MosaicId, S3KeyManager.ImageType.WebMosaic),
                    Expires = DateTime.UtcNow.AddHours(1)
                })
            };

            return summary;
        }
    }
}
