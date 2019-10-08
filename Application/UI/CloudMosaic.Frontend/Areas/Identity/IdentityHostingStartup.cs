using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(CloudMosaic.Frontend.Areas.Identity.IdentityHostingStartup))]
namespace CloudMosaic.Frontend.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
            });
        }
    }
}