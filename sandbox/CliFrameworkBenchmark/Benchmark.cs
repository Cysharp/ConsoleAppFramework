// This benchmark project is based on CliFx.Benchmarks.
// https://github.com/Tyrrrz/CliFx/tree/master/CliFx.Benchmarks/

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CliFx;
using Cocona.Benchmark.External.Commands;
using ConsoleAppFramework;
using Spectre.Console.Cli;

namespace Cocona.Benchmark.External;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class Benchmark
{
    private static readonly string[] Arguments = { "--str", "hello world", "-i", "13", "-b" };

    [Benchmark(Description = "Cocona.Lite")]
    public void ExecuteWithCoconaLite()
    {
        Cocona.CoconaLiteApp.Run<CoconaCommand>(Arguments);
    }

    [Benchmark(Description = "Cocona")]
    public void ExecuteWithCocona()
    {
        Cocona.CoconaApp.Run<CoconaCommand>(Arguments);
    }

    //[Benchmark(Description = "ConsoleAppFramework")]
    //public async ValueTask ExecuteWithConsoleAppFramework() =>
    //    await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<ConsoleAppFrameworkCommand>(Arguments);

    [Benchmark(Description = "CliFx")]
    public ValueTask<int> ExecuteWithCliFx()
    {
        return new CliApplicationBuilder().AddCommand(typeof(CliFxCommand)).Build().RunAsync(Arguments);
    }

    [Benchmark(Description = "System.CommandLine")]
    public int ExecuteWithSystemCommandLine()
    {
        return new SystemCommandLineCommand().Execute(Arguments);
    }

    //[Benchmark(Description = "McMaster.Extensions.CommandLineUtils")]
    //public int ExecuteWithMcMaster() =>
    //    McMaster.Extensions.CommandLineUtils.CommandLineApplication.Execute<McMasterCommand>(Arguments);

    //[Benchmark(Description = "CommandLineParser")]
    //public void ExecuteWithCommandLineParser() =>
    //    new Parser()
    //        .ParseArguments(Arguments, typeof(CommandLineParserCommand))
    //        .WithParsed<CommandLineParserCommand>(c => c.Execute());

    //[Benchmark(Description = "PowerArgs")]
    //public void ExecuteWithPowerArgs() =>
    //    PowerArgs.Args.InvokeMain<PowerArgsCommand>(Arguments);

    //[Benchmark(Description = "Clipr")]
    //public void ExecuteWithClipr() =>
    //    clipr.CliParser.Parse<CliprCommand>(Arguments).Execute();


    //[Benchmark(Description = "ConsoleAppFramework v5")]
    //public void ExecuteConsoleAppFramework5()
    //{
    //    ConsoleApp.Run(Arguments, ConsoleAppFrameworkCommand.Execute);
    //}

    [Benchmark(Description = "ConsoleAppFramework v5", Baseline = true)]
    public unsafe void ExecuteConsoleAppFramework()
    {
        ConsoleApp.Run(Arguments, &ConsoleAppFrameworkCommandWithCancellationToken.Execute);
    }

    // for alpha testing
    //private static readonly string[] TempArguments = { "", "--str", "hello world", "-i", "13", "-b" };
    //[Benchmark(Description = "ConsoleAppFramework.Builder")]
    //public unsafe void ExecuteConsoleAppFrameworkBuilder()
    //{
    //    var builder = ConsoleApp.Create();
    //    builder.Add("", ConsoleAppFrameworkCommand.Execute);
    //    builder.Run(TempArguments);
    //}

    [Benchmark(Description = "Spectre.Console.Cli")]
    public void ExecuteSpectreConsoleCli()
    {
        var app = new CommandApp<SpectreConsoleCliCommand>();
        app.Run(Arguments);
    }


    //[Benchmark(Description = "ConsoleAppFramework Builder API")]
    //public unsafe void ExecuteConsoleAppFramework2()
    //{
    //    var app = ConsoleApp.Create();
    //    app.Add("", ConsoleAppFrameworkCommand.Execute);
    //    app.Run(Arguments);
    //}

    //[Benchmark(Description = "ConsoleAppFramework CancellationToken")]
    //public unsafe void ExecuteConsoleAppFramework3()
    //{
    //    var app = ConsoleApp.Create();
    //    app.Add("", ConsoleAppFrameworkCommandWithCancellationToken.Execute);
    //    app.Run(Arguments);
    //}

    //[Benchmark(Description = "ConsoleAppFramework With Filter")]
    //public unsafe void ExecuteConsoleAppFramework4()
    //{
    //    var app = ConsoleApp.Create();
    //    app.UseFilter<NopConsoleAppFilter>();
    //    app.Add("", ConsoleAppFrameworkCommand.Execute);
    //    app.Run(Arguments);
    //}
}
