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

namespace Cocona.Benchmark.External;

class Program
{
    static void Main(string[] args)
    {
        var config = DefaultConfig.Instance
                                  .WithSummaryStyle(SummaryStyle.Default
                                  .WithTimeUnit(TimeUnit.Millisecond));

        config.AddDiagnoser(MemoryDiagnoser.Default);
        config.AddDiagnoser(ThreadingDiagnoser.Default);

        config.AddJob(Job.Default
                         .WithStrategy(RunStrategy.ColdStart)
                         .WithLaunchCount(1)
                         .WithWarmupCount(0)
                         .WithIterationCount(1)
                         .WithInvocationCount(1)
                         .WithToolchain(CsProjCoreToolchain.NetCoreApp80)
                         .DontEnforcePowerPlan());

        BenchmarkRunner.Run<Benchmark>(config, args);
    }
}
