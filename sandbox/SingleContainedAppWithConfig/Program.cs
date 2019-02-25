using MicroBatchFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        this.Context.Logger.LogTrace("Trace");
        this.Context.Logger.LogDebug("Debug");
        this.Context.Logger.LogInformation("Info");
        this.Context.Logger.LogWarning("Warning");
        this.Context.Logger.LogError("Error");
        this.Context.Logger.LogInformation($"GlobalValue: {config.Value.GlobalValue}, EnvValue: {config.Value.EnvValue}");
    }
}


namespace SingleContainedAppWithConfig
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await MicroBatchHost.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    // mapping json element to class
                    services.Configure<AppConfig>(hostContext.Configuration.GetSection("AppConfig"));
                })
                .ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Debug)) // Default is Info. You can change Log Level at here
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