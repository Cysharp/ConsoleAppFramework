﻿using ConsoleAppFramework;

[assembly: ConsoleAppFrameworkGeneratorOptions(DisableNamingConversion = true)]


args = ["HelloWorld", "--help"];
var app = ConsoleApp.Create();
//app.Add<MyCommands>();
app.Add<Tacommands>();
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



        //internal partial struct ConsoleAppBuilder
        //{
        //    /// <summary>
        //    /// Add all [RegisterCommands] types as ConsoleApp command.
        //    /// </summary>
        //    public void RegisterAll()
        //    {
        //    }

        //    /// <summary>
        //    /// Add all [RegisterCommands] types as ConsoleApp command.
        //    /// </summary>
        //    public void RegisterAll(string commandPath)
        //    {
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