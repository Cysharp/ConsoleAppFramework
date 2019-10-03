using MicroBatchFramework.WebHosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Collections.Generic;
using MicroBatchFramework.WebHosting.Swagger;
using System.IO;

namespace MicroBatchFramework // .WebHosting
{
    public static class BatchEngineHostingExtensions
    {
        public static IWebHostBuilder PrepareBatchEngineMiddleware(this IWebHostBuilder builder, IBatchInterceptor interceptor = null)
        {
            var batchTypes = CollectBatchTypes();
            var target = new TargetBatchTypeCollection(batchTypes);

            return builder
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IBatchInterceptor>(interceptor ?? NullBatchInterceptor.Default);
                    services.AddSingleton<TargetBatchTypeCollection>(target);
                    foreach (var item in target)
                    {
                        services.AddTransient(item);
                    }
                });
        }

        public static Task RunBatchEngineWebHosting(this IWebHostBuilder builder, string urls, SwaggerOptions swaggerOptions = null, IBatchInterceptor interceptor = null)
        {
            return builder
                .PrepareBatchEngineMiddleware(interceptor)
                .ConfigureServices(services =>
                {
                    if (swaggerOptions == null)
                    {
                        var entryAsm = Assembly.GetEntryAssembly();
                        var xmlName = entryAsm.GetName().Name + ".xml";
                        var xmlPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), xmlName);
                        swaggerOptions = new SwaggerOptions(entryAsm.GetName().Name, "", "/") { XmlDocumentPath = xmlPath };
                    }
                    services.AddSingleton<SwaggerOptions>(swaggerOptions);
                })
                .UseKestrel()
                .UseUrls(urls)
                .UseStartup<DefaultStartup>()
                .Build()
                .RunAsync();
        }

        public static IApplicationBuilder UseBatchEngineMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<BatchEngineMiddleware>();
        }

        public static IApplicationBuilder UseBatchEngineSwaggerMiddleware(this IApplicationBuilder builder, SwaggerOptions options)
        {
            return builder.UseMiddleware<BatchEngineSwaggerMiddleware>(options);
        }

        public class DefaultStartup
        {
            public void Configure(IApplicationBuilder app, IApplicationLifetime lifetime)
            {
                var interceptor = app.ApplicationServices.GetService<IBatchInterceptor>();
                var provider = app.ApplicationServices.GetService<IServiceProvider>();
                var logger = app.ApplicationServices.GetService<ILogger<BatchEngine>>();

                lifetime.ApplicationStarted.Register(async () =>
                {
                    try
                    {
                        await interceptor.OnBatchEngineBeginAsync(provider, logger);
                    }
                    catch { }
                });

                lifetime.ApplicationStopped.Register(async () =>
                {
                    try
                    {
                        await interceptor.OnBatchEngineEndAsync();
                    }
                    catch { }
                });

                var swaggerOption = app.ApplicationServices.GetService<SwaggerOptions>() ?? new SwaggerOptions("MicroBatchFramework", "", "/");
                app.UseBatchEngineSwaggerMiddleware(swaggerOption);
                app.UseBatchEngineMiddleware();
            }
        }

        static List<Type> CollectBatchTypes()
        {
            List<Type> batchBaseTypes = new List<Type>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.FullName.StartsWith("System") || asm.FullName.StartsWith("Microsoft.Extensions")) continue;

                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                foreach (var item in types)
                {
                    if (typeof(BatchBase).IsAssignableFrom(item) && item != typeof(BatchBase))
                    {
                        batchBaseTypes.Add(item);
                    }
                }
            }

            return batchBaseTypes;
        }
    }
}