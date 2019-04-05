using MicroBatchFramework.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;

namespace MicroBatchFramework
{
    // ref: https://github.com/aspnet/AspNetCore/blob/4e44025a52e4b73aa17e09a8041b0e166e0c5ce0/src/DefaultBuilder/src/WebHost.cs
    /// <summary>
    /// Provides convenience methods for creating instances of <see cref="IHost"/> and <see cref="IHostBuilder"/> with pre-configured defaults.
    /// </summary>
    public static class BatchHost
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="HostBuilder"/>:
        ///     set the <see cref="IHostingEnvironment.EnvironmentName"/> to the NETCORE_ENVIRONMENT,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostingEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostingEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     and configure the <see cref="SimpleConsoleLogger"/> to log to the console,
        /// </remarks>
        /// <returns>The initialized <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder CreateDefaultBuilder(bool useSimpleConosoleLogger = true) => CreateDefaultBuilder(useSimpleConosoleLogger, LogLevel.Debug);

        /// <summary>
        /// Initializes a new instance of the <see cref="HostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="HostBuilder"/>:
        ///     set the <see cref="IHostingEnvironment.EnvironmentName"/> to the NETCORE_ENVIRONMENT,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostingEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostingEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     and configure the <see cref="SimpleConsoleLogger"/> to log to the console,
        /// </remarks>
        /// <param name="useSimpleConosoleLogger"></param>
        /// <param name="minSimpleConsoleLoggerLogLevel"></param>
        /// <returns>The initialized <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder CreateDefaultBuilder(bool useSimpleConosoleLogger, LogLevel minSimpleConsoleLoggerLogLevel) => CreateDefaultBuilder(useSimpleConosoleLogger, minSimpleConsoleLoggerLogLevel, "");

        /// <summary>
        /// Initializes a new instance of the <see cref="HostBuilder"/> class with pre-configured defaults.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the returned <see cref="HostBuilder"/>:
        ///     set the <see cref="IHostingEnvironment.EnvironmentName"/> to the parameter of hostEnvironmentVariable,
        ///     load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostingEnvironment.EnvironmentName"/>].json',
        ///     load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostingEnvironment.EnvironmentName"/> is 'Development' using the entry assembly,
        ///     load <see cref="IConfiguration"/> from environment variables,
        ///     and configure the <see cref="SimpleConsoleLogger"/> to log to the console,
        /// </remarks>
        /// <param name="useSimpleConosoleLogger"></param>
        /// <param name="minSimpleConsoleLoggerLogLevel"></param>
        /// <param name="hostEnvironmentVariable"></param>
        /// <returns>The initialized <see cref="IHostBuilder"/>.</returns>
        public static IHostBuilder CreateDefaultBuilder(bool useSimpleConosoleLogger, LogLevel minSimpleConsoleLoggerLogLevel, string hostEnvironmentVariable)
        {
            var builder = new HostBuilder();

            builder.UseContentRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            ConfigureAppConfigurationDefault(builder, hostEnvironmentVariable);
            ConfigureLoggingDefault(builder, useSimpleConosoleLogger, minSimpleConsoleLoggerLogLevel);

            return builder;
        }

        internal static void ConfigureAppConfigurationDefault(IHostBuilder builder, string hostEnvironmentVariable)
        {
            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;
                env.ApplicationName = Assembly.GetExecutingAssembly().GetName().Name;
                if (string.IsNullOrWhiteSpace(hostEnvironmentVariable))
                {
                    hostEnvironmentVariable = "NETCORE_ENVIRONMENT";
                }
                env.EnvironmentName = System.Environment.GetEnvironmentVariable(hostEnvironmentVariable) ?? "Production";

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                if (env.IsDevelopment())
                {
                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    if (appAssembly != null)
                    {
                        // use https://marketplace.visualstudio.com/items?itemName=guitarrapc.OpenUserSecrets to easily manage UserSecrets with GenericHost.
                        config.AddUserSecrets(appAssembly, optional: true);
                    }
                }

                config.AddEnvironmentVariables();
            });
        }

        internal static void ConfigureLoggingDefault(IHostBuilder builder, bool useSimpleConosoleLogger, LogLevel minSimpleConsoleLoggerLogLevel)
        {
            if (useSimpleConosoleLogger)
            {
                builder.ConfigureLogging(logging =>
                {
                    logging.AddSimpleConsole();
                    logging.AddFilter<SimpleConsoleLoggerProvider>((category, level) =>
                    {
                        // omit system message
                        if (category.StartsWith("Microsoft.Extensions.Hosting.Internal"))
                        {
                            if (level <= LogLevel.Debug) return false;
                        }

                        return level >= minSimpleConsoleLoggerLogLevel;
                    });
                });
            }
        }
    }
}