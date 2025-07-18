﻿#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: ConsoleAppFrameworkGeneratorOptions(DisableNamingConversion = true)]

args = ["TestCommand", "Run"];

var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;

services.AddSingleton<ITest, Test>();
services.AddKeyedSingleton<ITest, KeyedTest>("Key");


MyObj obj = new();
while (obj.Data.Count < 1)
{
    obj.Data.Add(0); // <-- CAF008 error here
}

var app = builder.ToConsoleAppBuilder();
// var app = ConsoleApp.Create();
//for (int i = 0; i < 10; i++)
//{
//    app.Add("foo", (int x) => { });
//}

app.Run(args);

interface ITest
{
    int Value { get; set; }
}

class Test : ITest
{
    public int Value { get; set; } = 1;
}

class KeyedTest : ITest
{
    public int Value { get; set; } = 2;
}

[RegisterCommands(nameof(TestCommand))]
class TestCommand([FromKeyedServices("Key")] ITest test)
{
    public void Run()
    {
        // This value should be 2 but 1 displayed
        Console.WriteLine(test.Value);
    }
}

public partial class MyObj : ObservableObject
{
    [ObservableProperty]
    private List<int> data = [];
}


//args = ["echo", "--msg", "zzzz"];

//// IHostBuilder
//// HostApplicationBuilder
////var app = Host.CreateApplicationBuilder()
////    .ToConsoleAppBuilder();
//// appBuilder.Build();

//// Package Import: Microsoft.Extensions.Configuration.Json
//var app = ConsoleApp.Create()
//    .ConfigureDefaultConfiguration()
//    .ConfigureServices((configuration, services) =>
//    {
//        // Microsoft.Extensions.Options.ConfigurationExtensions
//        services.Configure<PositionOptions>(configuration.GetSection("Position"));
//    });

//// Uncomment following line to overwrite args.
// args = ["run", "--project", "foo.csproj", "--", "--foo", "100", "--bar", "bazbaz"];

// dotnet run --project foo.csproj -- --foo 100 --bar bazbaz

//var app = ConsoleApp.Create();

//app.Add("run", ([FromKeyedServices("takoyaki")] List<int> testList, string project, ConsoleAppContext context) =>
//{
//    // run --project foo.csproj -- --foo 100 --bar bazbaz
//    Console.WriteLine(string.Join(" ", context.Arguments));

//    // --project foo.csproj
//    Console.WriteLine(string.Join(" ", context.CommandArguments!));

//    //IServiceProvider ServiceProvider = null!;
//    // ((Microsoft.Extensions.DependencyInjection.IKeyedServiceProvider)ServiceProvider).GetKeyedService(Type, "");

//    // FromKeyedServicesAttribute
//    // IKeyedServiceProvider

//    // --foo 100 --bar bazbaz
//    Console.WriteLine(string.Join(" ", context.EscapedArguments!));
//});

//app.Add<MyCommands>("foo");

//app.UseFilter<NopFilter2>();

//app.Run(args);


//internal class NopFilter2([FromKeyedServices("mykey")] List<int> xxxx, ConsoleAppFilter next) : ConsoleAppFilter(next)
//{
//    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
//    {
//        try
//        {
//            /* on before */
//            await Next.InvokeAsync(context, cancellationToken); // next
//            /* on after */
//        }
//        catch
//        {
//            /* on error */
//            throw;
//        }
//        finally
//        {
//            /* on finally */
//        }
//    }
//}


////ConsoleApp.Run(args, (ConsoleAppContext ctx) => { });

//// inject options
////public class MyCommand(IOptions<PositionOptions> options)
////{
////    public void Echo(string msg)
////    {
////        ConsoleApp.Log($"Binded Option: {options.Value.Title} {options.Value.Name}");
////    }
////}

////public class PositionOptions
////{
////    public string Title { get; set; } = "";
////    public string Name { get; set; } = "";
////}

////internal class ServiceProviderScopeFilter(IServiceProvider serviceProvider, ConsoleAppFilter next) : ConsoleAppFilter(next)
////{
////    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
////    {
////        // create Microsoft.Extensions.DependencyInjection scope
////        await using var scope = serviceProvider.CreateAsyncScope();

////        var originalServiceProvider = ConsoleApp.ServiceProvider;
////        ConsoleApp.ServiceProvider = scope.ServiceProvider;
////        try
////        {
////            await Next.InvokeAsync(context, cancellationToken);
////        }
////        finally
////        {
////            ConsoleApp.ServiceProvider = originalServiceProvider;
////        }
////    }
////}

//// JsonSerializer.Deserialize<int>("foo");

////// inject logger to filter
////internal class ReplaceLogFilter(ConsoleAppFilter next, ILogger<Program> logger)
////    : ConsoleAppFilter(next)
////{
////    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
////    {
////        ConsoleApp.Log = msg => logger.LogInformation(msg);
////        ConsoleApp.LogError = msg => logger.LogError(msg);

////        return Next.InvokeAsync(context, cancellationToken);
////    }
////}

//class MyProvider : IServiceProvider, IAsyncDisposable
//{
//    public void Dispose()
//    {
//        Console.WriteLine("disposed");
//    }

//    public ValueTask DisposeAsync()
//    {
//        Console.WriteLine("dispose async");
//        return default;
//    }

//    public object? GetService(Type serviceType)
//    {
//        return null;
//    }
//}

//public class MyService
//{

//}


//public class MyCommands
//{
//    public MyCommands([FromKeyedServices(10.9)] float kokonimo)
//    {

//    }

//    public void Cmd1(int x, int y, ConsoleAppContext ctx)
//    {
//    }

//    public Task Cmd2([FromKeyedServices(typeof(int))] List<int> l, int x, int y)
//    {
//        return Task.CompletedTask;
//    }
//}

//public class Tacommands
//{
//    public void HelloWorld(int hogeMoge)
//    {
//    }
//}

//namespace ConsoleAppFramework
//{
//    internal static partial class ConsoleApp
//    {
//        static void Foo()
//        {
//            var options = JsonSerializerOptions ?? System.Text.Json.JsonSerializerOptions.Default;
//        }

//        //public static ConsoleAppBuilder Create(IServiceProvider serviceProvider)
//        //{
//        //    ConsoleApp.ServiceProvider = serviceProvider;
//        //    return ConsoleApp.Create();
//        //}

//        //public static ConsoleAppBuilder Create(Action<IServiceCollection> configure)
//        //{
//        //    var services = new ServiceCollection();
//        //    configure(services);
//        //    ConsoleApp.ServiceProvider = services.BuildServiceProvider();
//        //    return ConsoleApp.Create();
//        //}



//        //internal partial class ConsoleAppBuilder
//        //{
//        //    bool requireConfiguration;
//        //    IConfiguration? configuration;
//        //    Action<IConfiguration, IServiceCollection>? configureServices;
//        //    Action<IConfiguration, ILoggingBuilder>? configureLogging;

//        //    /// <summary>Create configuration with SetBasePath(Directory.GetCurrentDirectory()) and AddJsonFile("appsettings.json").</summary>
//        //    public void ConfigureDefaultConfiguration(Action<IConfigurationBuilder> configure)
//        //    {
//        //        var config = new ConfigurationBuilder();
//        //        config.SetBasePath(System.IO.Directory.GetCurrentDirectory());
//        //        config.AddJsonFile("appsettings.json", optional: true);
//        //        configure(config);
//        //        configuration = config.Build();
//        //    }

//        //    public void ConfigureEmptyConfiguration(Action<IConfigurationBuilder> configure)
//        //    {
//        //        var config = new ConfigurationBuilder();
//        //        configure(config);
//        //        configuration = config.Build();
//        //    }

//        //    public void ConfigureServices(Action<IServiceCollection> configure)
//        //    {
//        //        this.configureServices = (_, services) => configure(services);
//        //    }

//        //    public void ConfigureServices(Action<IConfiguration, IServiceCollection> configure)
//        //    {
//        //        this.requireConfiguration = true;
//        //        this.configureServices = configure;
//        //    }

//        //    public void ConfigureLogging(Action<ILoggingBuilder> configure)
//        //    {
//        //        this.configureLogging = (_, builder) => configure(builder);
//        //    }

//        //    public void ConfigureLogging(Action<IConfiguration, ILoggingBuilder> configure)
//        //    {
//        //        this.requireConfiguration = true;
//        //        this.configureLogging = configure;
//        //    }

//        //    public void BuildAndSetServiceProvider()
//        //    {
//        //        if (configureServices == null && configureLogging == null) return;

//        //        if (configureServices != null)
//        //        {
//        //            var services = new ServiceCollection();
//        //            configureServices?.Invoke(configuration!, services);

//        //            if (configureLogging != null)
//        //            {
//        //                var config = configuration;
//        //                if (requireConfiguration && config == null)
//        //                {
//        //                    config = new ConfigurationRoot(Array.Empty<IConfigurationProvider>());
//        //                }

//        //                var configure = configureLogging;
//        //                services.AddLogging(logging =>
//        //                {
//        //                    configure!(config!, logging);
//        //                });
//        //            }

//        //            ConsoleApp.ServiceProvider = services.BuildServiceProvider();
//        //        }
//        //    }
//        //}
//    }


//}




//namespace HogeHoge
//{



//    public class BatchAttribute : Attribute
//    {
//    }


//    public class Batch2Attribute : BatchAttribute
//    {
//    }


//}
