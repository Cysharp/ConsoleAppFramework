using ConsoleAppFramework.WebHosting;
using ConsoleAppFramework.WebHosting.Swagger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace ConsoleAppFramework // .WebHosting
{
    public static class ConsoleAppEngineHostingExtensions
    {
        public static IWebHostBuilder PrepareConsoleAppEngineMiddleware(this IWebHostBuilder builder, IConsoleAppInterceptor? interceptor = null)
        {
            var consoleAppTypes = CollectConsoleAppTypes();
            var target = new TargetConsoleAppTypeCollection(consoleAppTypes);

            return builder
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConsoleAppInterceptor>(interceptor ?? NullConsoleAppInterceptor.Default);
                    services.AddSingleton<TargetConsoleAppTypeCollection>(target);
                    foreach (var item in target)
                    {
                        services.AddTransient(item);
                    }
                });
        }

        public static Task RunConsoleAppEngineWebHosting(this IWebHostBuilder builder, string urls, SwaggerOptions? swaggerOptions = null, IConsoleAppInterceptor? interceptor = null)
        {
            return builder
                .PrepareConsoleAppEngineMiddleware(interceptor)
                .ConfigureServices(services =>
                {
                    if (swaggerOptions == null)
                    {
                        // GetEntryAssembly() never returns null when called from managed code.
                        var entryAsm = Assembly.GetEntryAssembly()!;
                        var xmlName = entryAsm.GetName().Name + ".xml";
                        var xmlPath = Path.Combine(Path.GetDirectoryName(entryAsm.Location) ?? "", xmlName);
                        swaggerOptions = new SwaggerOptions(entryAsm.GetName().Name!, "", "/") { XmlDocumentPath = xmlPath };
                    }
                    services.AddSingleton<SwaggerOptions>(swaggerOptions);
                })
                .UseKestrel()
                .UseUrls(urls)
                .UseStartup<DefaultStartup>()
                .Build()
                .RunAsync();
        }

        public static IApplicationBuilder UseConsoleAppEngineMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ConsoleAppEngineMiddleware>();
        }

        public static IApplicationBuilder UseConsoleAppEngineSwaggerMiddleware(this IApplicationBuilder builder, SwaggerOptions options)
        {
            return builder.UseMiddleware<ConsoleAppEngineSwaggerMiddleware>(options);
        }

        public class DefaultStartup
        {
            public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime)
            {
                var interceptor = app.ApplicationServices.GetService<IConsoleAppInterceptor>();
                var provider = app.ApplicationServices.GetService<IServiceProvider>();
                var logger = app.ApplicationServices.GetService<ILogger<ConsoleAppEngine>>();

                lifetime.ApplicationStarted.Register(async () =>
                {
                    try
                    {
                        await interceptor.OnConsoleAppEngineBeginAsync(provider, logger);
                    }
                    catch { }
                });

                lifetime.ApplicationStopped.Register(async () =>
                {
                    try
                    {
                        await interceptor.OnConsoleAppEngineEndAsync();
                    }
                    catch { }
                });

                var swaggerOption = app.ApplicationServices.GetService<SwaggerOptions>() ?? new SwaggerOptions("ConsoleAppFramework", "", "/");
                app.UseConsoleAppEngineSwaggerMiddleware(swaggerOption);
                app.UseConsoleAppEngineMiddleware();
            }
        }

        static List<Type> CollectConsoleAppTypes()
        {
            List<Type> consoleAppBaseTypes = new List<Type>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!(asm.FullName is null)
                    && (asm.FullName.StartsWith("System") || asm.FullName.StartsWith("Microsoft.Extensions"))) continue;

                Type[]? types;
                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // If Reflection cannot load a class, Types will be null.
                    types = ex.Types;
                }

                if (types is null) continue;
                foreach (var item in types)
                {
                    if (typeof(ConsoleAppBase).IsAssignableFrom(item) && item != typeof(ConsoleAppBase))
                    {
                        consoleAppBaseTypes.Add(item);
                    }
                }
            }

            return consoleAppBaseTypes;
        }
    }
}