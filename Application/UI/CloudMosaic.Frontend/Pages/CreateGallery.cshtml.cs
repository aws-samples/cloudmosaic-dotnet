using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using CloudMosaic.Frontend.Models;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.IO;

namespace CloudMosaic.Frontend.Pages
{
    public class CreateGalleryModel : PageModel
    {
        DynamoDBContext _ddbContext;
        private MosaicManager _importJobMananger;

        public CreateGalleryModel(IAmazonDynamoDB ddbClient, MosaicManager importJobManager)
        {
            this._ddbContext = new DynamoDBContext(ddbClient, new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2});
            this._importJobMananger = importJobManager;
        }

        [BindProperty]
        [Required]
        public string Name { get; set; }

        [BindProperty]
        [Required]
        public IFormFile GalleryTilesSource { get; set; }

        [BindProperty]
        public string Attributions { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            var gallery = new Gallery
            {
                UserId = this.HttpContext.User.Identity.Name,
                GalleryId = $"{this.Name}-{Guid.NewGuid().ToString()}",
                Name = this.Name,
                Attributions = this.Attributions,
                IsPublic = false,
                CreateDate = DateTime.UtcNow,
                Status = Gallery.Statuses.Importing
            };

            using (var stream = GalleryTilesSource.OpenReadStream())
            {
                await this._importJobMananger.StartGalleryImport(gallery.UserId, gallery.GalleryId, stream).ConfigureAwait(false);
            }

            await this._ddbContext.SaveAsync(gallery).ConfigureAwait(false);

            return RedirectToPage("Galleries");
        }
    }
}