﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace ConsoleAppFramework
{
    public class ConsoleAppBuilder : IHostBuilder
    {
        readonly IHostBuilder builder;

        internal ConsoleAppBuilder(string[] args, IHostBuilder hostBuilder)
            : this(args, hostBuilder, (_, __) => { })
        {
        }

        internal ConsoleAppBuilder(string[] args, IHostBuilder hostBuilder, ConsoleAppOptions consoleAppOptions)
        {
            this.builder = AddConsoleAppFramework(hostBuilder, args, consoleAppOptions, null);
        }

        internal ConsoleAppBuilder(string[] args, IHostBuilder hostBuilder, Action<ConsoleAppOptions> configureOptions)
            : this(args, hostBuilder, (_, options) => configureOptions(options))
        {
        }

        internal ConsoleAppBuilder(string[] args, IHostBuilder hostBuilder, Action<HostBuilderContext, ConsoleAppOptions> configureOptions)
        {
            this.builder = AddConsoleAppFramework(hostBuilder, args, new ConsoleAppOptions(), configureOptions);
        }

        IHostBuilder AddConsoleAppFramework(IHostBuilder builder, string[] args, ConsoleAppOptions options, Action<HostBuilderContext, ConsoleAppOptions>? configureOptions)
        {
            return builder
                .ConfigureServices((ctx, services) =>
                {
                    services.AddOptions<ConsoleLifetimeOptions>().Configure(x => x.SuppressStatusMessages = true);
                    services.AddHostedService<ConsoleAppEngineService>();
                    configureOptions?.Invoke(ctx, options);
                    options.CommandLineArguments = args;
                    services.AddSingleton(options);
                    services.AddSingleton<IParamsValidator, ParamsValidator>();

                    if (options.ReplaceToUseSimpleConsoleLogger)
                    {
                        services.AddLogging(builder =>
                        {
                            builder.ReplaceToSimpleConsole();
                        });
                    }
                })
                .UseConsoleLifetime();
        }

        // IHostBuilder implementations::

        public IDictionary<object, object> Properties => builder.Properties;

        IHost IHostBuilder.Build()
        {
            return builder.Build();
        }

        IHostBuilder IHostBuilder.ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            return builder.ConfigureAppConfiguration(configureDelegate);
        }

        IHostBuilder IHostBuilder.ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            return builder.ConfigureContainer(configureDelegate);
        }

        IHostBuilder IHostBuilder.ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            return builder.ConfigureHostConfiguration(configureDelegate);
        }

        IHostBuilder IHostBuilder.ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            return builder.ConfigureServices(configureDelegate);
        }

        IHostBuilder IHostBuilder.UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
        {
            return builder.UseServiceProviderFactory(factory);
        }

        IHostBuilder IHostBuilder.UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
        {
            return builder.UseServiceProviderFactory(factory);
        }

        // override implementations that returns ConsoleAppBuilder

        public ConsoleApp Build()
        {
            var host = builder.Build();
            return new ConsoleApp(host);
        }

        public ConsoleAppBuilder ConfigureAppConfiguration(Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            builder.ConfigureAppConfiguration(configureDelegate);
            return this;
        }

        public ConsoleAppBuilder ConfigureContainer<TContainerBuilder>(Action<HostBuilderContext, TContainerBuilder> configureDelegate)
        {
            builder.ConfigureContainer(configureDelegate);
            return this;
        }

        public ConsoleAppBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            builder.ConfigureHostConfiguration(configureDelegate);
            return this;
        }

        public ConsoleAppBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            builder.ConfigureServices(configureDelegate);
            return this;
        }

        public ConsoleAppBuilder UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
            where TContainerBuilder : notnull
        {
            builder.UseServiceProviderFactory(factory);
            return this;
        }

        public ConsoleAppBuilder UseServiceProviderFactory<TContainerBuilder>(Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
            where TContainerBuilder : notnull
        {
            builder.UseServiceProviderFactory(factory);
            return this;
        }

        // Override Configure methods(Microsoft.Extensions.Hosting.HostingHostBuilderExtensions) tor return ConsoleAppBuilder

        public ConsoleAppBuilder ConfigureLogging(Action<ILoggingBuilder> configureLogging)
        {
            (this as IHostBuilder).ConfigureLogging(configureLogging);
            return this;
        }

        public ConsoleAppBuilder ConfigureLogging(Action<HostBuilderContext, ILoggingBuilder> configureLogging)
        {
            (this as IHostBuilder).ConfigureLogging(configureLogging);
            return this;
        }

        public ConsoleAppBuilder UseEnvironment(string environment)
        {
            (this as IHostBuilder).UseEnvironment(environment);
            return this;
        }

        public ConsoleAppBuilder UseContentRoot(string contentRoot)
        {
            (this as IHostBuilder).UseContentRoot(contentRoot);
            return this;
        }

        public ConsoleAppBuilder UseDefaultServiceProvider(Action<ServiceProviderOptions> configure)
        {
            (this as IHostBuilder).UseDefaultServiceProvider(configure);
            return this;
        }

        public ConsoleAppBuilder UseDefaultServiceProvider(Action<HostBuilderContext, ServiceProviderOptions> configure)
        {
            (this as IHostBuilder).UseDefaultServiceProvider(configure);
            return this;
        }

        public ConsoleAppBuilder ConfigureHostOptions(Action<HostBuilderContext, HostOptions> configureOptions)
        {
            (this as IHostBuilder).ConfigureHostOptions(configureOptions);
            return this;
        }

        public ConsoleAppBuilder ConfigureHostOptions(Action<HostOptions> configureOptions)
        {
            (this as IHostBuilder).ConfigureHostOptions(configureOptions);
            return this;
        }

        public ConsoleAppBuilder ConfigureAppConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            (this as IHostBuilder).ConfigureAppConfiguration(configureDelegate);
            return this;
        }

        public ConsoleAppBuilder ConfigureServices(Action<IServiceCollection> configureDelegate)
        {
            (this as IHostBuilder).ConfigureServices(configureDelegate);
            return this;
        }

        public ConsoleAppBuilder ConfigureContainer<TContainerBuilder>(Action<TContainerBuilder> configureDelegate)
        {
            (this as IHostBuilder).ConfigureContainer(configureDelegate);
            return this;
        }
    }

    public static class HostBuilderExtensions
    {
        public static ConsoleApp BuildAsConsoleApp(this IHostBuilder hostBuilder)
        {
            var app = hostBuilder.Build() as ConsoleApp;
            if (app == null)
            {
                throw new InvalidOperationException($"HostBuilder is not ConsoleAppBuilder.");
            }
            return app;
        }
    }
}