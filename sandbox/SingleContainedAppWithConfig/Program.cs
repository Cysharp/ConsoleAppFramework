using MicroBatchFramework;
using MicroBatchFramework.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        // https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel?view=aspnetcore-2.2
        this.Context.Logger.LogTrace("Trace"); // 0
        this.Context.Logger.LogDebug("Debug"); // 1
        this.Context.Logger.LogInformation("Info"); // 2
        this.Context.Logger.LogWarning("Warning"); // 3
        this.Context.Logger.LogError("Error"); // 4
        this.Context.Logger.LogInformation($"GlobalValue: {config.Value.GlobalValue}, EnvValue: {config.Value.EnvValue}");
    }
}


namespace SingleContainedAppWithConfig
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // using MicroBatchFramework.Configuration;
            await MicroBatchHost.CreateDefaultBuilder(args, LogLevel.Debug)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    // mapping json element to class
                    services.Configure<AppConfig>(hostContext.Configuration.GetSection("AppConfig"));
                })
                .RunBatchEngineAsync<Baz>(args);
        }
    }

    // config mapping class
    public class AppConfig
    {
        public string GlobalValue { get; set; }
        public string EnvValue { get; set; }
    }
}