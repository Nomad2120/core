using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OSI.PaymentService
{
    public class NginxSwaggerMiddleware
    {
        private readonly PathString nginxPathBase;
        private readonly RequestDelegate _next;
        private readonly ILogger<NginxSwaggerMiddleware> _logger;

        public NginxSwaggerMiddleware(PathString nginxPathBase, RequestDelegate next, ILogger<NginxSwaggerMiddleware> logger)
        {
            this.nginxPathBase = nginxPathBase;
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var oldPath = context.Request.Path;
            var oldPathBase = context.Request.PathBase;
            if (context.Request.Path.StartsWithSegments(nginxPathBase))
            {
                context.Request.Path = context.Request.Path.ToString().Remove(0, nginxPathBase.ToString().Length);
                if (!context.Request.Path.StartsWithSegments("/swagger"))
                {
                    context.Request.Path = new PathString("/api").Add(context.Request.Path);
                }
                context.Request.PathBase = context.Request.PathBase.Add(nginxPathBase);
            }
            await _next(context);
            context.Request.Path = oldPath;
            context.Request.PathBase = oldPathBase;
        }

        public static Action<OpenApiDocument, HttpRequest> SwaggerPreserializeFilter(PathString nginxPathBase)
        {
            return (doc, request) =>
            {
                if (request.PathBase == nginxPathBase)
                {
                    doc.Servers.Clear();
                    foreach (var path in doc.Paths.Keys.ToArray())
                    {
                        if (path.StartsWith("/api"))
                        {
                            doc.Paths.Remove(path, out var value);
                            doc.Paths.Add(nginxPathBase.ToString() + path.Remove(0, 4), value);
                        }
                    }
                }
            };
        }
    }

    public static class NginxSwaggerBuilderExtensions
    {
        public static IApplicationBuilder UseNginxSwagger(this IApplicationBuilder app, PathString nginxPathBase, SwaggerOptions options)
        {
            app.UseMiddleware<NginxSwaggerMiddleware>(nginxPathBase);
            options.PreSerializeFilters.Add(NginxSwaggerMiddleware.SwaggerPreserializeFilter(nginxPathBase));
            app.UseSwagger(options);
            return app;
        }

        public static IApplicationBuilder UseNginxSwagger(this IApplicationBuilder app, PathString nginxPathBase, Action<SwaggerOptions> setupAction = null)
        {
            app.UseMiddleware<NginxSwaggerMiddleware>(nginxPathBase);
            app.UseSwagger(options =>
            {
                setupAction?.Invoke(options);
                options.PreSerializeFilters.Add(NginxSwaggerMiddleware.SwaggerPreserializeFilter(nginxPathBase));
            });
            return app;
        }
    }
}
