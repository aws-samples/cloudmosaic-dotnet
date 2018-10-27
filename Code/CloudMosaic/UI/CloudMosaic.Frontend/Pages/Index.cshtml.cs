using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;

using CloudMosaic.Frontend.Models;
using CloudMosaic.Common;

using Microsoft.Extensions.Options;

namespace CloudMosaic.Frontend.Pages
{
    public class IndexModel : PageModel
    {


        public void OnGet()
        {
        }
    }
}
