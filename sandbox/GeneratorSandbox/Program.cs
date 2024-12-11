using ConsoleAppFramework;
using FilterShareProject;
using Microsoft.Extensions.DependencyInjection;


args = ["Output"];

var app = ConsoleApp.Create();

// ConsoleApp.ServiceProvider
// ConsoleApp.Create(

ConsoleApp.Create(sc =>
{
    
});

app.Add<MyCommands>();

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
    }
}