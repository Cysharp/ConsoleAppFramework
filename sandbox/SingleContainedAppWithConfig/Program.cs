using MicroBatchFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

public class Baz : BatchBase
{
    private readonly IOptions<SingleContainedAppWithConfig.AppConfig> config;
    // Batche inject Config on constructor.
    public Baz(IOptions<SingleContainedAppWithConfig.AppConfig> config)
    {
        this.config = config;
    }

    public void Hello3()
    {
        this.Context.Logger.LogInformation($"GlobalValue: {config.Value.GlobalValue}, EnvValue: {config.Value.EnvValue}");
    }
}


namespace SingleContainedAppWithConfig
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new HostBuilder()
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    // Set Environment variable "NETCORE_ENVIRONMENT" as Production | Staging | Development
                    hostContext.HostingEnvironment.EnvironmentName = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT") ?? "production";
                    config.SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                        .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional:true, reloadOnChange:true);
                })
                .ConfigureServices((hostContext, services) => {
                    services.AddOptions();
                    // mapping json element to class
                    services.Configure<AppConfig>(hostContext.Configuration.GetSection("AppConfig"));
                })
                .ConfigureLogging(x => x.AddConsole())
                .RunBatchEngine<Baz>(args);
        }
    }

    public class batchInterceptor : IBatchInterceptor
    {
        Stopwatch start;

        public ValueTask OnBatchEngineBegin()
        {
            start = Stopwatch.StartNew();
            return default(ValueTask);
        }

        public ValueTask OnBatchEngineEnd()
        {
            Console.WriteLine(start.Elapsed.TotalMilliseconds + "ms");
            return default(ValueTask);
        }

        public ValueTask OnBatchRunBegin(BatchContext context)
        {
            Console.WriteLine("Yeah3");
            return default(ValueTask);
        }

        public ValueTask OnBatchRunComplete(BatchContext context, string errorMessageIfFailed, Exception exceptionIfExists)
        {
            Console.WriteLine("Yeah4");
            return default(ValueTask);
        }
    }

    // config mapping class
    public class AppConfig
    {
        public string GlobalValue { get; set; }
        public string EnvValue { get; set; }
    }
}
