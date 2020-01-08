using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

public class Baz : ConsoleAppBase
{
    private readonly IOptions<SingleContainedAppWithConfig.AppConfig> config;
    // Batche inject Config on constructor.
    public Baz(IOptions<SingleContainedAppWithConfig.AppConfig> config, MyServiceA serviceA, MyServiceB serviceB, MyServiceC serviceC)
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

public class MyServiceA { }
public class MyServiceB { }
public class MyServiceC { }

namespace SingleContainedAppWithConfig
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // using ConsoleAppFramework.Configuration;
            await ConsoleAppFramework.ConsoleAppHost.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    // mapping json element to class
                    services.Configure<AppConfig>(hostContext.Configuration.GetSection("AppConfig"));

                    services.AddScoped<MyServiceA>();
                    services.AddTransient<MyServiceB>();
                    services.AddSingleton<MyServiceC>();
                })
                .RunConsoleAppEngineAsync<Baz>(args);
        }
    }

    // config mapping class
    public class AppConfig
    {
        public string GlobalValue { get; set; }
        public string EnvValue { get; set; }
    }
}