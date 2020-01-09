using ConsoleAppFramework.WebHosting.Swagger;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ConsoleAppFramework.WebHosting
{
    public class ConsoleAppFrameworkSwaggerMiddleware
    {
        static readonly Task EmptyTask = Task.FromResult(0);

        readonly RequestDelegate next;
        readonly MethodInfo[] handlers;
        readonly SwaggerOptions options;

        public ConsoleAppFrameworkSwaggerMiddleware(RequestDelegate next, TargetConsoleAppTypeCollection targetTypes, SwaggerOptions options)
        {
            this.next = next;
            this.handlers = targetTypes.SelectMany(x => x.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)).ToArray();
            this.options = options;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            // reference embedded resouces
            const string prefix = "ConsoleAppFramework.WebHosting.Swagger.SwaggerUI.";

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

            var myAssembly = typeof(ConsoleAppFrameworkSwaggerMiddleware).GetTypeInfo().Assembly;

            using (var stream = myAssembly.GetManifestResourceStream(filePath))
            {
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
                        using (var ms = new MemoryStream())
                        {
                            await stream.CopyToAsync(ms);
                            bytes = options.ResolveCustomResource(path, ms.ToArray());
                        }
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
        }

        static string GetMediaType(string path)
        {
            var extension = path.Split('.').Last();

            switch (extension)
            {
                case "css":
                    return "text/css";
                case "js":
                    return "text/javascript";
                case "json":
                    return "application/json";
                case "gif":
                    return "image/gif";
                case "png":
                    return "image/png";
                case "eot":
                    return "application/vnd.ms-fontobject";
                case "woff":
                    return "application/font-woff";
                case "woff2":
                    return "application/font-woff2";
                case "otf":
                    return "application/font-sfnt";
                case "ttf":
                    return "application/font-sfnt";
                case "svg":
                    return "image/svg+xml";
                case "ico":
                    return "image/x-icon";
                default:
                    return "text/html";
            }
        }
    }
}
