using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

using CloudMosaic.Frontend.Models;

namespace CloudMosaic.Frontend.Pages
{
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
            var search = this._ddbContext.QueryAsync<Gallery>(Constants.DEFAULT_USER_ID);

            this.Galleries = await search.GetRemainingAsync();
        }
    }
}