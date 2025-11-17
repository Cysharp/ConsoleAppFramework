// This benchmark project is based on CliFx.Benchmarks.
// https://github.com/Tyrrrz/CliFx/tree/master/CliFx.Benchmarks/

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.CsProj;
using Perfolizer.Horology;

namespace CliFrameworkBenchmarks;

class Program
{
    static void Main(string[] args)
    {
        var config = DefaultConfig.Instance
                                  .WithSummaryStyle(SummaryStyle.Default
                                  .WithTimeUnit(TimeUnit.Millisecond))
                                  .HideColumns(BenchmarkDotNet.Columns.Column.Error)
                                  ;

        config.AddDiagnoser(MemoryDiagnoser.Default);
        // config.AddDiagnoser(new ThreadingDiagnoser(new ThreadingDiagnoserConfig(displayLockContentionWhenZero: false, displayCompletedWorkItemCountWhenZero: false)));

        config.AddJob(Job.Default
                         .WithStrategy(RunStrategy.ColdStart)
                         .WithLaunchCount(1)
                         .WithWarmupCount(0)
                         .WithIterationCount(1)
                         .WithInvocationCount(1)
                         .WithToolchain(CsProjCoreToolchain.NetCoreApp10_0) // .NET 10
                         .DontEnforcePowerPlan());

        BenchmarkRunner.Run<Benchmark>(config, args);
    }
}
