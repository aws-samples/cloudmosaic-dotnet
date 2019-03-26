using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Amazon.AspNetCore.DataProtection.SSM;

using Amazon;
using Amazon.Util;
using Microsoft.Extensions.Logging;

namespace CloudMosaic.Frontend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            ConfigureDynamoDB();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppOptions>(Configuration.GetSection("AppOptions"));

            services.AddAWSService<Amazon.Batch.IAmazonBatch>();
            services.AddAWSService<Amazon.DynamoDBv2.IAmazonDynamoDB>();
            services.AddAWSService<Amazon.ECS.IAmazonECS>();
            services.AddAWSService<Amazon.S3.IAmazonS3>();
            services.AddAWSService<Amazon.StepFunctions.IAmazonStepFunctions>();
            services.AddAWSService<Amazon.SimpleSystemsManagement.IAmazonSimpleSystemsManagement>();

            services.AddSingleton<MosaicManager>();


            //services.AddDbContext<ApplicationDbContext>(options =>
            //    options.UseSqlServer(
            //        Configuration.GetConnectionString("DefaultConnection")));
            //services.AddDefaultIdentity<IdentityUser>()
            //    .AddEntityFrameworkStores<ApplicationDbContext>();


            services.AddCognitoIdentity();

            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = new PathString("/Identity/Account/Login");
            });


            services.AddDataProtection()
                .PersistKeysToAWSSystemsManager("/CloudMosaic/DataProtection");


            services.AddMvc()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                    .AddRazorPagesOptions(o =>
                    {
                        o.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute());
                    });
        }

        private void ConfigureDynamoDB()
        {
            string value;
            if ((value = this.Configuration["AppOptions:TableGallery"]) != null)
            {
                AWSConfigsDynamoDB.Context.AddMapping(new TypeMapping(typeof(Models.Gallery), value));
            }
            if ((value = this.Configuration["AppOptions:TableMosaic"]) != null)
            {
                AWSConfigsDynamoDB.Context.AddMapping(new TypeMapping(typeof(Models.Mosaic), value));
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddAWSProvider(this.Configuration.GetAWSLoggingConfigSection());

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseAuthentication();

            app.UseMvc();
        }
    }
}
