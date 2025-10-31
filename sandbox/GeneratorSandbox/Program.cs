using ConsoleAppFramework;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

//args = ["--x", "10", "--y", "20", "-v", "--prefix-output", "takoyakix"];

var app = ConsoleApp.Create();

//
// AddGlobalOption

app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    var verbose = builder.AddGlobalOption<bool>($"-v", "");
    var noColor = builder.AddGlobalOption<bool>("--no-color", "Don't colorize output.");
    var dryRun = builder.AddGlobalOption<bool>("--dry-run", "");
    var prefixOutput = builder.AddRequiredGlobalOption<string>("--prefix-output|-pp|-po", "Prefix output with level.");

    // var tako = builder.AddGlobalOption<int>("--in", "");
    //var tako = builder.AddGlobalOption<MyFruit>("--fruit", "");

    //return new GlobalOptions(true, true, true, "");
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

app.Add<MyCommand>();

// app.Add("", (int x, int y, [FromServices] GlobalOptions globalOptions) => Console.WriteLine(x + y + ":" + globalOptions));

//var iii = int.Parse("1000");
//var sss = new string('a', 3);
//var datet  = DateTime.Parse("10000");

// AddGlobalOption<int?>("hoge", "takoyaki", null);

app.Run(args);


// public T AddGlobalOption<T>([ConstantExpected] string name, [ConstantExpected] string description = "", T defaultValue = default(T))


static void AddGlobalOption<T>([ConstantExpected] string name, [ConstantExpected] string description, [ConstantExpected] T defaultValue)
{
}

internal record GlobalOptions(bool Verbose, bool NoColor, bool DryRun, string PrefixOutput);

internal class MyCommand(GlobalOptions globalOptions)
{
    /// <summary>
    /// my command
    /// </summary>
    /// <param name="xxx">-x, takoyaki</param>
    /// <param name="yyyy">-yy|-y, naninuneno</param>
    [Command("")]
    public void Run(int xxx, int yyyy, bool z, bool zzz, Int128 iiii, int? takoyaki = null, MyFruit myFruit = default, Version version = null)
    {
        Console.WriteLine(xxx + yyyy + ":" + globalOptions);
    }
}

public enum MyFruit
{
    Apple, Orange, Grape
}


