using ConsoleAppFramework;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

args = ["cmd-a", "--x", "10", "--y", "20", "--int-parameter", "1000"];

var app = ConsoleApp.Create();

// builder: Func<ref ConsoleApp.GlobalOptionsBuilder, object>
app.ConfigureGlobalOptions((ref builder) =>
{
    var dryRun = builder.AddGlobalOption<bool>("--dry-run");
    var verbose = builder.AddGlobalOption<bool>("-v|--verbose");
    var intParameter = builder.AddRequiredGlobalOption<int>("--int-parameter", "integer parameter");

    // return value stored to ConsoleAppContext.GlobalOptions
    return new GlobalOptions(dryRun, verbose, intParameter);
});

app.ConfigureServices((context, configuration, services) =>
{
    // store global-options to DI
    var globalOptions = (GlobalOptions)context.GlobalOptions;
    services.AddSingleton(globalOptions);

    // check global-options value to configure services
    services.AddLogging(logging =>
    {
        if (globalOptions.Verbose)
        {
            logging.SetMinimumLevel(LogLevel.Trace);
        }
    });
});

app.Add<Commands>();

app.Run(args);

internal record GlobalOptions(bool DryRun, bool Verbose, int IntParameter);

// get GlobalOptions from DI
internal class Commands(GlobalOptions globalOptions)
{
    [Command("cmd-a")]
    public void CommandA(int x, int y)
    {
        Console.WriteLine("A:" + globalOptions + ":" + (x, y));
    }

    [Command("cmd-b")]
    public void CommandB(int x, int y)
    {
        Console.WriteLine("B:" + globalOptions + ":" + (x, y));
    }
}
