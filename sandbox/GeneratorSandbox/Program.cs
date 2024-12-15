#nullable enable

using ConsoleAppFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using ZLogger;

[assembly: ConsoleAppFrameworkGeneratorOptions(DisableNamingConversion = true)]



var app = ConsoleApp.Create()
    ;

app.ConfigureDefaultConfiguration();

app.ConfigureServices(services =>
{

});

  //  .ConfigureLogging(
   // .ConfigureDefaultConfiguration()
  //  ;

app.Add("", () => { });

app.Run(args);



public class MyProjectCommand
{
    public void Execute(int x)
    {
        Console.WriteLine("Hello?");
    }
}


public class MyCommands
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="msg">foobarbaz!</param>
    [Command("Error1")]
    public void Error1(string msg = @"\")
    {
        Console.WriteLine(msg);
    }
    [Command("Error2")]
    public void Error2(string msg = "\\")
    {
        Console.WriteLine(msg);
    }
    [Command("Output")]
    public void Output(string msg = @"\\")
    {
        Console.WriteLine(msg); // 「\」
    }
}

public class Tacommands
{
    public void HelloWorld(int hogeMoge)
    {
    }
}

namespace ConsoleAppFramework
{
    internal static partial class ConsoleApp
    {
        //public static ConsoleAppBuilder Create(IServiceProvider serviceProvider)
        //{
        //    ConsoleApp.ServiceProvider = serviceProvider;
        //    return ConsoleApp.Create();
        //}

        //public static ConsoleAppBuilder Create(Action<IServiceCollection> configure)
        //{
        //    var services = new ServiceCollection();
        //    configure(services);
        //    ConsoleApp.ServiceProvider = services.BuildServiceProvider();
        //    return ConsoleApp.Create();
        //}



        //internal partial class ConsoleAppBuilder
        //{
        //    bool requireConfiguration;
        //    IConfiguration? configuration;
        //    Action<IConfiguration, IServiceCollection>? configureServices;
        //    Action<IConfiguration, ILoggingBuilder>? configureLogging;

        //    /// <summary>Create configuration with SetBasePath(Directory.GetCurrentDirectory()) and AddJsonFile("appsettings.json").</summary>
        //    public void ConfigureDefaultConfiguration(Action<IConfigurationBuilder> configure)
        //    {
        //        var config = new ConfigurationBuilder();
        //        config.SetBasePath(System.IO.Directory.GetCurrentDirectory());
        //        config.AddJsonFile("appsettings.json", optional: true);
        //        configure(config);
        //        configuration = config.Build();
        //    }

        //    public void ConfigureEmptyConfiguration(Action<IConfigurationBuilder> configure)
        //    {
        //        var config = new ConfigurationBuilder();
        //        configure(config);
        //        configuration = config.Build();
        //    }

        //    public void ConfigureServices(Action<IServiceCollection> configure)
        //    {
        //        this.configureServices = (_, services) => configure(services);
        //    }

        //    public void ConfigureServices(Action<IConfiguration, IServiceCollection> configure)
        //    {
        //        this.requireConfiguration = true;
        //        this.configureServices = configure;
        //    }

        //    public void ConfigureLogging(Action<ILoggingBuilder> configure)
        //    {
        //        this.configureLogging = (_, builder) => configure(builder);
        //    }

        //    public void ConfigureLogging(Action<IConfiguration, ILoggingBuilder> configure)
        //    {
        //        this.requireConfiguration = true;
        //        this.configureLogging = configure;
        //    }

        //    public void BuildAndSetServiceProvider()
        //    {
        //        if (configureServices == null && configureLogging == null) return;

        //        if (configureServices != null)
        //        {
        //            var services = new ServiceCollection();
        //            configureServices?.Invoke(configuration!, services);

        //            if (configureLogging != null)
        //            {
        //                var config = configuration;
        //                if (requireConfiguration && config == null)
        //                {
        //                    config = new ConfigurationRoot(Array.Empty<IConfigurationProvider>());
        //                }

        //                var configure = configureLogging;
        //                services.AddLogging(logging =>
        //                {
        //                    configure!(config!, logging);
        //                });
        //            }

        //            ConsoleApp.ServiceProvider = services.BuildServiceProvider();
        //        }
        //    }
        //}
    }


}




namespace HogeHoge
{



    public class BatchAttribute : Attribute
    {
    }


    public class Batch2Attribute : BatchAttribute
    {
    }


    [RegisterCommands, Batch]
    public class Takoyaki
    {
        public void Error12345()
        {
        }
    }
}