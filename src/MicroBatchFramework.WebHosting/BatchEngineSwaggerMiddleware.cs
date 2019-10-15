using MicroBatchFramework.WebHosting.Swagger;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MicroBatchFramework.WebHosting
{
    public class BatchEngineSwaggerMiddleware
    {
        private readonly RequestDelegate next;
        private readonly MethodInfo[] handlers;
        private readonly SwaggerOptions options;

        public BatchEngineSwaggerMiddleware(RequestDelegate next, TargetBatchTypeCollection targetTypes, SwaggerOptions options)
        {
            this.next = next;
            this.handlers = targetTypes.SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)).ToArray();
            this.options = options;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            // reference embedded resouces
            const string prefix = "MicroBatchFramework.WebHosting.Swagger.SwaggerUI.";

            var path = httpContext.Request.Path.Value.Trim('/');
            if (path == "") path = "index.html";
            var filePath = prefix + path.Replace("/", ".");
            var mediaType = GetMediaType(filePath);

            if (path.EndsWith(options.JsonName))
            {
                var builder = new SwaggerDefinitionBuilder(options, httpContext, handlers);
                var bytes = builder.BuildSwaggerJson();
                httpContext.Response.Headers["Content-Type"] = new[] { "application/json" };
                httpContext.Response.StatusCode = 200;
                await httpContext.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                return;
            }

            var myAssembly = typeof(BatchEngineSwaggerMiddleware).GetTypeInfo().Assembly;

            using var stream = myAssembly.GetManifestResourceStream(filePath);
            if (options.ResolveCustomResource == null)
            {
                if (stream == null)
                {
                    // not found, standard request.
                    await next(httpContext);
                    return;
                }

                httpContext.Response.Headers["Content-Type"] = new[] { mediaType };
                httpContext.Response.StatusCode = 200;
                var response = httpContext.Response.Body;
                await stream.CopyToAsync(response);
            }
            else
            {
                byte[] bytes;
                if (stream == null)
                {
                    bytes = options.ResolveCustomResource(path, null);
                }
                else
                {
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    bytes = options.ResolveCustomResource(path, ms.ToArray());
                }

                if (bytes == null)
                {
                    // not found, standard request.
                    await next(httpContext);
                    return;
                }

                httpContext.Response.Headers["Content-Type"] = new[] { mediaType };
                httpContext.Response.StatusCode = 200;
                var response = httpContext.Response.Body;
                await response.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        private static string GetMediaType(string path)
        {
            var extension = path.Split('.').Last();

            return extension switch
            {
                "css" => "text/css",
                "js" => "text/javascript",
                "json" => "application/json",
                "gif" => "image/gif",
                "png" => "image/png",
                "eot" => "application/vnd.ms-fontobject",
                "woff" => "application/font-woff",
                "woff2" => "application/font-woff2",
                "otf" => "application/font-sfnt",
                "ttf" => "application/font-sfnt",
                "svg" => "image/svg+xml",
                "ico" => "image/x-icon",
                _ => "text/html",
            };
        }
    }
}
