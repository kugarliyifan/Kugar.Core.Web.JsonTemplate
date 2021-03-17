using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using NSwag;
using NSwag.Generation.Processors.Security;

namespace Kugar.Core.Web.JsonTemplate.Test
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddLocalization(new CultureInfo("zh-cn"));

            //services.EnableSyncIO();

            services.AddSwaggerDocument(opt =>
            {
                //opt.DocumentName = "api";
                // opt.ApiGroupNames = new[] { "wxapi" };
                opt.DocumentName = "wxapi";
                opt.Title = "微信小程序接口";

                AppDomain.CurrentDomain.GetAssemblies();

                opt.AddJsonTemplateV2(typeof(Startup).Assembly);
                opt.PostProcess = (doc) =>
                {
                    doc.Consumes = new string[] {"application/json"};
                    doc.Produces = new string[] {"application/json"};
                };

                opt.DocumentProcessors.Add(new SecurityDefinitionAppender("Authorization", new OpenApiSecurityScheme()
                {
                    Type = OpenApiSecuritySchemeType.ApiKey,
                    Name = "Authorization",
                    In = OpenApiSecurityApiKeyLocation.Header,
                    Description = "授权token",

                }));
            });

            services.EnableSyncIO();
            
            services.AddControllers().AddNewtonsoftJson(opt =>
            {
                opt.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                opt.SerializerSettings.ContractResolver ??= new DefaultContractResolver()
                {
                    NamingStrategy = new CamelCaseNamingStrategy(true, true)
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
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseJsonTemplate();

            app.UseOpenApi();       // serve OpenAPI/Swagger documents

            //app.UseSwaggerUi3();    // serve Swagger UI

            app.UseSwaggerUi3(config =>  // serve ReDoc UI
            {
                // 這裡的 Path 用來設定 ReDoc UI 的路由 (網址路徑) (一定要以 / 斜線開頭)
                config.Path = "/swager";
            });


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
