using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using CloudMosaic.Frontend.Models;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

namespace CloudMosaic.Frontend.Pages
{
    public class CreateGalleryModel : PageModel
    {
        DynamoDBContext _ddbContext;
        private ImportJobManager _importJobMananger;

        public CreateGalleryModel(IAmazonDynamoDB ddbClient, ImportJobManager importJobManager)
        {
            this._ddbContext = new DynamoDBContext(ddbClient, new DynamoDBContextConfig { Conversion = DynamoDBEntryConversion.V2});
            this._importJobMananger = importJobManager;
        }

        [BindProperty]
        [Required]
        public string Name { get; set; }

        [BindProperty]
        public string Attributions { get; set; }

        [BindProperty]
        [Required]
        public string ImportUrl { get; set; }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPostAsync()
        {
            var gallery = new Gallery
            {
                UserId = Constants.DEFAULT_USER_ID,
                GalleryId = $"{this.Name}-{Guid.NewGuid().ToString()}",
                Name = this.Name,
                Attributions = this.Attributions,
                Status = Gallery.Statuses.Importing
            };

            await this._importJobMananger.StartImport(gallery.UserId, gallery.GalleryId, this.ImportUrl);

            await this._ddbContext.SaveAsync(gallery);

            return RedirectToPage("Galleries");
        }
    }
}