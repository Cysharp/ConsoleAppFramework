using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

args = ["--x", "10", "--y", "20", "-v", "--prefix-output", "takoyakix"];

var app = ConsoleApp.Create();

app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    var verbose = builder.AddGlobalOption<bool>($"-v", "", true);
    var noColor = builder.AddGlobalOption<bool>("--no-color", "Don't colorize output.");
    var dryRun = builder.AddGlobalOption<bool>("--dry-run");
    var prefixOutput = builder.AddRequiredGlobalOption<string>("--prefix-output|-pp|-po", "Prefix output with level.");

    return new GlobalOptions(verbose, noColor, dryRun, prefixOutput);
});

app.ConfigureServices((context, configuration, collection) =>
{
    var globalOptions = (GlobalOptions)context.GlobalOptions;

    // simply use for filter/command body
    collection.AddSingleton(globalOptions);

    // variable for setup other DI
    collection.AddLogging(logging =>
    {
        var console = logging.AddSimpleConsole();
        if (globalOptions.Verbose)
        {
            console.SetMinimumLevel(LogLevel.Trace);
        }
    });
});

app.Add("", (int x, int y, [FromServices]GlobalOptions globalOptions) => Console.WriteLine(x + y + ":" + globalOptions));

app.Run(args);

internal record GlobalOptions(bool Verbose, bool NoColor, bool DryRun, string PrefixOutput);
