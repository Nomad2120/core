using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OSI.Core.Logging;
using OSI.Core.Models;
using OSI.Core.Swagger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSI.PaymentService
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

            services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "OSI.PaymentService", Version = "v1" });
                c.SchemaFilter<SwaggerIgnoreSchemaFilter>();
                c.OperationFilter<SwaggerIgnoreOperationFilter>();
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetAssembly(typeof(Program)).GetName().Name}.xml"), true);
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetAssembly(typeof(ApiResponse)).GetName().Name}.xml"), true);
            });

            services.AddHttpContextAccessor();
            services.AddTransient<HttpLoggingHandler>();
            services
                .AddHttpClient(Options.DefaultName)
                .AddHttpMessageHandler<HttpLoggingHandler>();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                static IPNetwork ParseIPNetwork(string ipNetwork)
                {
                    string[] ipNetworkParts = ipNetwork.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    return new IPNetwork(IPAddress.Parse(ipNetworkParts[0]), int.Parse(ipNetworkParts.ElementAtOrDefault(1) ?? "32"));
                }

                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                foreach (var ipNetwork in Configuration.GetSection("ForwardedHeadersOptions:KnownNetworks").Get<string[]>().Select(x => ParseIPNetwork(x)))
                {
                    options.KnownNetworks.Add(ipNetwork);
                }
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseForwardedHeaders();

            app.UseNginxSwagger(new PathString(Configuration["NginxPathBase"]));
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("v1/swagger.json", "OSI.PaymentService v1");
                c.EnableDeepLinking();
            });

            //app.UseHttpsRedirection();

            app.UseRouting();

            app.UseMiddleware<BankCodeIpLoggingMiddleware>();
            app.UseMiddleware<RequestResponseLoggingMiddleware>();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
