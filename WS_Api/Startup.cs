using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using api.Business;
using api.Model;

namespace api
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
            services.AddControllers();

            // 🔹 Add CORS policy
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()  // Allows any domain (for development; specify in production)
                           .AllowAnyMethod()  // Allows GET, POST, PUT, DELETE, etc.
                           .AllowAnyHeader(); // Allows all headers
                });
            });

            // 🔹 Enable Form Data Parsing
            services.Configure<FormOptions>(options =>
            {
                options.ValueCountLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = int.MaxValue;
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();

            app.UseDefaultFiles(); // Enables default file serving (like index.html)
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            //Add the middleware to inspect the raw request body HERE:
            //app.Use(async (context, next) =>
            //{
            //    context.Request.EnableBuffering(); // Enable buffering to allow reading the body multiple times.
            //    var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            //    context.Request.Body.Position = 0; // Rewind the body stream.
            //    //string ipAddress = Reports.GetIP(context.Request, context.Connection);
            //    WSData.SaveRawData(body, "TEST");
            //    //System.Diagnostics.Debug.WriteLine($"Raw Request Body: {body}");
            //    await next();
            //});

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }

    }
}
