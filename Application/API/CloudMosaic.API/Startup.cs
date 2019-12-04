using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Amazon;
using Amazon.Util;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSwag;
using NSwag.Generation.Processors.Contexts;
using NSwag.Generation.Processors.Security;

namespace CloudMosaic.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Amazon.XRay.Recorder.Handlers.AwsSdk.AWSSDKHandler.RegisterXRayForAllServices();

            Configuration = configuration;
            ConfigureDynamoDB();
        }

        public IConfiguration Configuration { get; }

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

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppOptions>(Configuration.GetSection("AppOptions"));

            services.AddAWSService<Amazon.Batch.IAmazonBatch>();
            services.AddAWSService<Amazon.DynamoDBv2.IAmazonDynamoDB>();
            services.AddAWSService<Amazon.S3.IAmazonS3>();
            services.AddAWSService<Amazon.StepFunctions.IAmazonStepFunctions>();

            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var region = Configuration["AWS:Region"];
                    if(string.IsNullOrEmpty(region))
                    {
                        region = Amazon.Runtime.FallbackRegionFactory.GetRegionEndpoint().SystemName;
                    }

                    var audience = Configuration["AWS:UserPoolClientId"];
                    var authority = $"https://cognito-idp.{region}.amazonaws.com/" + Configuration["AWS:UserPoolId"];

                    Console.WriteLine($"Configure JWT option, Audience: {audience}, Authority: {authority}");
                    

                    options.Audience = audience;
                    options.Authority = authority;
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy =>
                    policy.RequireClaim("cognito:groups", "Admin"));
            });

            services.AddControllers();

            // Register the Swagger services
            services.AddSwaggerDocument(document =>
            {
                // Add an authenticate button to Swagger for JWT tokens
                document.OperationProcessors.Add(new OperationSecurityScopeProcessor("JWT"));
                document.DocumentProcessors.Add(new SecurityDefinitionAppender("JWT", new OpenApiSecurityScheme
                {
                    Type = OpenApiSecuritySchemeType.ApiKey,
                    Name = "Authorization",                    
                    In = OpenApiSecurityApiKeyLocation.Header,
                    Description = "Type into the textbox: Bearer {your JWT token}. You can get a JWT token from /Authorization/Authenticate."
                }));

                // Post process the generated document
                document.PostProcess = d =>
                {
                    d.Info.Title = "CloudMosaic API";
                    d.Info.Description = "API to manage tile gallaries and mosaic images.";
                    d.Info.Version = "1.0.0";
                };
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseXRay("CloudMosaic.API");

            // Register the Swagger generator and the Swagger UI middlewares
            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
