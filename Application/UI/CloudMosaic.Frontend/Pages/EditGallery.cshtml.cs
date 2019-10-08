using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using CloudMosaic.Frontend.Models;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using System.Net;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace CloudMosaic.Frontend.Pages
{
    public class EditGalleryModel : PageModel
    {
        DynamoDBContext _ddbContext;
        private MosaicManager _importJobMananger;

        public EditGalleryModel(IAmazonDynamoDB ddbClient, MosaicManager importJobManager)
        {
            this._ddbContext = new DynamoDBContext(ddbClient, new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2});
            this._importJobMananger = importJobManager;
        }

        [BindProperty]
        [Required]
        public string GalleryId { get; set; }

        [BindProperty]
        public string Name { get; set; }

        [BindProperty]
        public string Attributions { get; set; }

        [BindProperty]
        public IFormFile GalleryTilesSource { get; set; }
        
        [BindProperty]
        public bool IsPublic { get; set; }

        public async Task OnGet(string galleryId)
        {
            this.GalleryId = galleryId;
            var gallery = await _ddbContext.LoadAsync<Gallery>(HttpContext.User.Identity.Name, GalleryId).ConfigureAwait(false);

            this.Name = gallery.Name;
            this.Attributions = gallery.Attributions;
            this.IsPublic = gallery.IsPublic;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var gallery = new Gallery
            {
                UserId = this.HttpContext.User.Identity.Name,
                GalleryId = this.GalleryId,
                Name = this.Name,
                Attributions = this.Attributions,
                IsPublic = this.IsPublic
            };

            if (GalleryTilesSource != null)
            {
                using (var stream = GalleryTilesSource.OpenReadStream())
                {
                    gallery.Status = Gallery.Statuses.Importing;
                    await this._importJobMananger.StartGalleryImport(gallery.UserId, gallery.GalleryId, stream).ConfigureAwait(false);
                }
            }

            await this._ddbContext.SaveAsync(gallery).ConfigureAwait(false);

            return RedirectToPage("Galleries");
        }
    }
}