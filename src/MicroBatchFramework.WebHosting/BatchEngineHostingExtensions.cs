using MicroBatchFramework.WebHosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Collections.Generic;

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
                    services.AddSingleton<IBatchInterceptor>(interceptor ?? NullBatchInerceptor.Default);
                    services.AddSingleton<TargetBatchTypeCollection>(target);
                    foreach (var item in target)
                    {
                        services.AddTransient(item);
                    }
                });
        }

        public static Task RunBatchEngineWebHosting(this IWebHostBuilder builder, string urls, IBatchInterceptor interceptor = null)
        {
            return builder
                .PrepareBatchEngineMiddleware(interceptor)
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