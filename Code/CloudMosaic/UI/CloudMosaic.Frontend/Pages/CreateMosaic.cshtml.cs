using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using CloudMosaic.Frontend.Models;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.IO;
using CloudMosaic.Common;

namespace CloudMosaic.Frontend.Pages
{
    public class CreateMosaicModel : PageModel
    {
        DynamoDBContext _ddbContext;
        MosaicManager _mosaicManager;

        public CreateMosaicModel(IAmazonDynamoDB ddbClient, MosaicManager mosaicManager)
        {
            this._ddbContext = new DynamoDBContext(ddbClient, new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2 });
            this._mosaicManager = mosaicManager;
        }

        public IList<Gallery> Galleries { get; set; }

        [BindProperty]
        public string Name { get; set; }

        [BindProperty]
        [Required]
        public string GalleryId { get; set; }

        [BindProperty]
        [Required]
        public IFormFile MosaicSourceImage { get; set; }

        public async Task OnGetAsync()
        {
            var search = this._ddbContext.QueryAsync<Gallery>(this.HttpContext.User.Identity.Name);

            this.Galleries = await search.GetRemainingAsync().ConfigureAwait(false);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var fileName = WebUtility.HtmlEncode(
                Path.GetFileName(MosaicSourceImage.FileName));
            var extension = Path.GetExtension(fileName);

            if (MosaicSourceImage.Length > Constants.MAX_SOURCE_IMAGE_SIZE)
            {
                return BadRequest($"{fileName} is larger then the max size of {Constants.MAX_SOURCE_IMAGE_SIZE}");
            }
            if(!string.Equals(".jpg", extension, StringComparison.OrdinalIgnoreCase) && !string.Equals(".png", extension, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest($"File types {extension} are not supported, only jpg and png files");
            }

            using (var stream = MosaicSourceImage.OpenReadStream())
            {
                await this._mosaicManager.CreateMosaic(this.HttpContext.User.Identity.Name, this.GalleryId, this.Name ?? fileName, stream).ConfigureAwait(false);
            }

            return RedirectToPage("Mosaics");
        }
    }
}