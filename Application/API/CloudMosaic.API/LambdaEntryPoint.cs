using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace CloudMosaic.API
{
    public class LambdaEntryPoint :
        Amazon.Lambda.AspNetCoreServer.ApplicationLoadBalancerFunction
    {
        /// <summary>
        /// The builder has configuration, logging and Amazon API Gateway already configured. The startup class
        /// needs to be configured in this method using the UseStartup&lt;&gt;() method.
        /// </summary>
        /// <param name="builder"></param>
        protected override void Init(IWebHostBuilder builder)
        {
            builder
                .ConfigureAppConfiguration(builder =>
                {
                    builder.AddSystemsManager("/CloudMosaic");
                })
                .UseStartup<Startup>();
        }
    }
}
