using ConsoleAppFramework;
using GeneratorSandbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// fail
//await ConsoleApp.RunAsync(args, Commands.Save);



//var app = ConsoleApp.Create();


//args = ["--x", "10", "--y", "20", "-v", "--prefix-output", "takoyakix"];


//// Enum.TryParse<Fruit>("", true,
//// parse immediately


//app.ConfigureGlobalOption(x =>
//{
//    var verbose = x.AddGlobalOption<bool>($"takoyaki", "", true);
//    var noColor = x.AddGlobalOption<bool>("--no-color", "Don't colorize output.");
//    var dryRun = x.AddGlobalOption<bool>("--dry-run");
//    var prefixOutput = x.AddRequiredGlobalOption<string>("--prefix-output|-pp|-po", "Prefix output with level.");

//    return new GlobalOptions(verbose, noColor, dryRun, prefixOutput);
//});

//app.ConfigureServices(x =>
//{

//    // new ConsoleAppContext("",






//    // to use command body
//    //x.AddSingleton<GlobalOptions>(new GlobalOptions(verbose, noColor, dryRun, prefixOutput));

//    //// variable for setup other DI
//    //x.AddLogging(l =>
//    //{
//    //    var console = l.AddSimpleConsole();
//    //    if (verbose)
//    //    {
//    //        console.SetMinimumLevel(LogLevel.Trace);
//    //    }
//    //});
//});

//app.Add<Commands>("");

//app.Run(args);

//var app = ConsoleApp.Create();


//app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
//{
//    var verbose = builder.AddGlobalOption<bool>($"-v", "", true);
//    var noColor = builder.AddGlobalOption<bool>("--no-color", "Don't colorize output.");
//    var dryRun = builder.AddGlobalOption<bool>("--dry-run");
//    var prefixOutput = builder.AddRequiredGlobalOption<string>("--prefix-output|-pp|-po", "Prefix output with level.");

//    return new GlobalOptions(verbose, noColor, dryRun, prefixOutput);
//});


//app.Add("", async (int x, int y, ConsoleAppContext context, CancellationToken cancellationToken) =>
//{
//    Console.WriteLine("OK");
//    await Task.Delay(TimeSpan.FromSeconds(1));
//    Console.WriteLine(context.CommandName + ":" + (x, y));
//});

//app.Add("tako", (int x, int y, ConsoleAppContext context) =>
//{
//    Console.WriteLine(context.CommandName);
//});

//app.UseFilter<NopFilter>();

//await app.RunAsync(args);

var builder = ConsoleApp.Create();

builder.UseFilter<NopFilter1>();
builder.UseFilter<NopFilter2>();

builder.Add<MyClass>();

await builder.RunAsync(args);

[ConsoleAppFilter<NopFilter3>]
[ConsoleAppFilter<NopFilter4>]
public class MyClass
{
    [ConsoleAppFilter<NopFilter5>]
    [ConsoleAppFilter<NopFilter6>]
    public void Hello()
    {
        Console.Write("abcde");
    }
}

internal class NopFilter1(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.Write(1);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter2(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.Write(2);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter3(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.Write(3);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter4(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.Write(4);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter5(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.Write(5);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter6(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.Write(6);
        return Next.InvokeAsync(context, cancellationToken);
    }
}


public record GlobalOptions(bool Verbose, bool NoColor, bool DryRun, string PrefixOutput);


internal delegate object TakoyakiX(FooStruct builder);


public enum Fruit
{
    Orange, Apple, Grape
}




public ref struct FooStruct
{
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







            private void RunCommand0_2(string[] args, int commandDepth, int escapeIndex, Action<int, int, global::ConsoleAppFramework.ConsoleAppContext> command, CancellationToken __ExternalCancellationToken__)
            {
                var commandArgs = (escapeIndex == -1) ? args.AsSpan(commandDepth) : args.AsSpan(commandDepth, escapeIndex - commandDepth);
                if (TryShowHelpOrVersion(commandArgs, 2, 0)) return;

                ConsoleAppContext context = default!;
                //if (configureGlobalOptions == null)
                //{
                //    context = new ConsoleAppContext("", args, null, null, commandDepth, escapeIndex);
                //}
                //else
                //{
                //    var builder = new GlobalOptionsBuilder(commandArgs);
                //    var globalOptions = configureGlobalOptions(ref builder);
                //    context = new ConsoleAppContext("", args, null, globalOptions, commandDepth, escapeIndex);
                //    commandArgs = builder.RemainingArgs;
                //}
                //BuildAndSetServiceProvider(context);

                var arg0 = default(int);
                var arg0Parsed = false;
                var arg1 = default(int);
                var arg1Parsed = false;
                var arg2 = context;

                try
                {
                    for (int i = 0; i < commandArgs.Length; i++)
                    {
                        var name = commandArgs[i];

                        switch (name)
                        {
                            case "--x":
                                {
                                    if (!TryIncrementIndex(ref i, commandArgs.Length) || !int.TryParse(commandArgs[i], out arg0)) { ThrowArgumentParseFailed("x", commandArgs[i]); }
                                    arg0Parsed = true;
                                    continue;
                                }
                            case "--y":
                                {
                                    if (!TryIncrementIndex(ref i, commandArgs.Length) || !int.TryParse(commandArgs[i], out arg1)) { ThrowArgumentParseFailed("y", commandArgs[i]); }
                                    arg1Parsed = true;
                                    continue;
                                }
                            default:
                                if (string.Equals(name, "--x", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!TryIncrementIndex(ref i, commandArgs.Length) || !int.TryParse(commandArgs[i], out arg0)) { ThrowArgumentParseFailed("x", commandArgs[i]); }
                                    arg0Parsed = true;
                                    continue;
                                }
                                if (string.Equals(name, "--y", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!TryIncrementIndex(ref i, commandArgs.Length) || !int.TryParse(commandArgs[i], out arg1)) { ThrowArgumentParseFailed("y", commandArgs[i]); }
                                    arg1Parsed = true;
                                    continue;
                                }
                                ThrowArgumentNameNotFound(name);
                                break;
                        }
                    }
                    if (!arg0Parsed) ThrowRequiredArgumentNotParsed("x");
                    if (!arg1Parsed) ThrowRequiredArgumentNotParsed("y");

                    command(arg0!, arg1!, arg2!);
                }
                catch (Exception ex)
                {
                    Environment.ExitCode = 1;
                    if (ex is ValidationException or ArgumentParseFailedException)
                    {
                        LogError(ex.Message);
                    }
                    else
                    {
                        LogError(ex.ToString());
                    }
                }
            }


        }



    }
}
