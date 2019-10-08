using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CloudMosaic.Frontend.Pages
{
    public class AboutModel : PageModel
    {
        public string Message { get; set; }

        public void OnGet()
        {
            Message = "CloudMosaic - an example of a modern serverless .NET Core web application on Amazon Web Services.";
        }
    }
}
