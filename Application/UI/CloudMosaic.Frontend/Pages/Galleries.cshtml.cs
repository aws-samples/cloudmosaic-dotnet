using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

using CloudMosaic.Frontend.Models;

namespace CloudMosaic.Frontend.Pages
{
    [Authorize]
    public class GalleriesModel : PageModel
    {
        DynamoDBContext _ddbContext;

        public IList<Gallery> Galleries { get; set; }

        public GalleriesModel(IAmazonDynamoDB ddbClient)
        {
            this._ddbContext = new DynamoDBContext(ddbClient);
        }

        public async Task OnGetAsync()
        {
            var search = this._ddbContext.QueryAsync<Gallery>(this.HttpContext.User.Identity.Name);

            this.Galleries = await search.GetRemainingAsync().ConfigureAwait(false);
        }
    }
}