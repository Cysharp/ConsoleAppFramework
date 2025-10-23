using ConsoleAppFramework;
using GeneratorSandbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

// fail
//await ConsoleApp.RunAsync(args, Commands.Save);


var app = ConsoleApp.Create();

// parse immediately
var verbose = app.AddGlobalOptions<bool>(ref args, "-v|--verbose");
var noColor = app.AddGlobalOptions<bool>(ref args, "--no-color", "Don't colorize output.");
var dryRun = app.AddGlobalOptions<bool>(ref args, "--dry-run");
var prefixOutput = app.AddRequiredGlobalOptions<string>(ref args, "--prefix-output", "Prefix output with level.");

app.ConfigureServices(x =>
{
    // to use command body
    x.AddSingleton<GlobalOptions>(new GlobalOptions(verbose, noColor, dryRun, prefixOutput));

    // variable for setup other DI
    x.AddLogging(l =>
    {
        var console = l.AddSimpleConsole();
        if (verbose)
        {
            console.SetMinimumLevel(LogLevel.Trace);
        }
    });
});

app.Add<Commands>("");

app.Run(args);

record GlobalOptions(bool Verbose, bool NoColor, bool DryRun, string PrefixOutput);


public class Commands
{
    /// <summary>
    /// Some sort of save command.
    /// </summary>
    public async Task<int> Save(int x, int y)
    {

        await Task.Delay(1000);
        return 0;
    }
}


// `using var posixSignalHandler = PosixSignalHandler.Register(Timeout);`

namespace ConsoleAppFramework
{
    internal static partial class ConsoleApp
    {
        internal partial class ConsoleAppBuilder
        {
            public T AddGlobalOptions<T>(ref string[] args, string name, string description = "", T defaultValue = default(T))
                where T : IParsable<T>
            {
                return default(T);
            }

            public T AddRequiredGlobalOptions<T>(ref string[] args, [ConstantExpected] string name, [ConstantExpected] string description = "")
                where T : IParsable<T>
            {
                if (typeof(T) == typeof(bool)) throw new ArgumentException();

                var aliasCount = name.AsSpan().Count("|") + 1;
                if (aliasCount == 1)
                {
                    // TryParse...
                }
                else
                {
                    string[] aliases = name.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();


                }

                //for (int i = 0; i < args.Length; i++)
                //{
                //    if (args[i] == name)
                //    {
                //        args.AsSpan().Count("|");
                //    }
                //}



                return default(T);
            }
        }
    }
}
