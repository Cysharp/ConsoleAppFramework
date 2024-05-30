// This benchmark project is based on CliFx.Benchmarks.
// https://github.com/Tyrrrz/CliFx/tree/master/CliFx.Benchmarks/

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;
using CliFx;
using Cocona.Benchmark.External.Commands;
using CommandLine;
using ConsoleAppFramework;
using PowerArgs;
using Spectre.Console.Cli;
using System.ComponentModel.DataAnnotations.Schema;
using BenchmarkDotNet.Columns;

namespace Cocona.Benchmark.External;

// use ColdStart strategy to measure startup time evaluation
[SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 0, iterationCount: 1, invocationCount: 1)]
[MemoryDiagnoser]
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
        return SystemCommandLineCommand.Execute(Arguments);
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
        ConsoleApp.Run(Arguments, &ConsoleAppFrameworkCommand.Execute);
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
}