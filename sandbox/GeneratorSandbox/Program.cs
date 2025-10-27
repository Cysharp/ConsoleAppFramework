using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// fail
//await ConsoleApp.RunAsync(args, Commands.Save);



var app = ConsoleApp.Create();


args = ["--x", "10", "--y", "20", "-f", "Orange", "-v", "--prefix-output", "takoyakix"];


// Enum.TryParse<Fruit>("", true,
// parse immediately



app.ConfigureGlobalOption(x =>
{
    var verbose = x.AddGlobalOption<bool>($"takoyaki", "", true);
    var noColor = x.AddGlobalOption<bool>("--no-color", "Don't colorize output.");
    var dryRun = x.AddGlobalOption<bool>("--dry-run");
    var prefixOutput = x.AddRequiredGlobalOption<string>("--prefix-output|-pp|-po", "Prefix output with level.");

    return (verbose, noColor, dryRun, prefixOutput);
});

app.ConfigureServices(x =>
{

    // new ConsoleAppContext("",






    // to use command body
    //x.AddSingleton<GlobalOptions>(new GlobalOptions(verbose, noColor, dryRun, prefixOutput));

    //// variable for setup other DI
    //x.AddLogging(l =>
    //{
    //    var console = l.AddSimpleConsole();
    //    if (verbose)
    //    {
    //        console.SetMinimumLevel(LogLevel.Trace);
    //    }
    //});
});

app.Add<Commands>("");

app.Run(args);


static T ParseArgumentEnum<T>(ref string[] args, int i)
    where T : struct, Enum
{
    if ((i + 1) < args.Length)
    {
        if (Enum.TryParse<T>(args[i + 1], out var value))
        {
            //RemoveRange(ref args, i, 2);
            return value;
        }

        //ThrowArgumentParseFailed(args[i], args[i + 1]);
    }
    else
    {
        // ThrowArgumentParseFailed(args[i], "");
    }

    return default;
}

public record GlobalOptions(bool Verbose, bool NoColor, bool DryRun, string PrefixOutput);


public enum Fruit
{
    Orange, Apple, Grape
}


public class Commands(GlobalOptions globalOptions)
{
    /// <summary>
    /// Some sort of save command.
    /// </summary>
    public async Task<int> Save(int x, int y, ConsoleAppContext ctx)
    {
        Console.WriteLine(globalOptions);
        Console.WriteLine("Called this:" + new { x, y });
        Console.WriteLine(ctx.CommandName);
        await Task.Delay(1000);
        return 0;
    }
}


namespace ConsoleAppFramework
{
    internal static partial class ConsoleApp
    {
        internal partial class ConsoleAppBuilder
        {

        }

    }
}
