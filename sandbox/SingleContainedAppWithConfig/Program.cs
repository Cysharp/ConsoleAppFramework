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
    private readonly IOptions<SingleContainedApp.AppConfig> config;
    public Baz(IOptions<SingleContainedApp.AppConfig> config)
    {
        this.config = config;
    }
    public void Hello3()
    {
        this.Context.Logger.LogInformation(config.Value.MyValue);
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

    public class AppConfig
    {
        public string MyValue { get; set; }
    }
}
