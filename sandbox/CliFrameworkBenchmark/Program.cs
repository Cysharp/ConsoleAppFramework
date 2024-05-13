// This benchmark project is based on CliFx.Benchmarks.
// https://github.com/Tyrrrz/CliFx/tree/master/CliFx.Benchmarks/

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Cocona.Benchmark.External;

class Program
{
    static void Main(string[] args)
    {
#pragma warning disable CS0618
        BenchmarkRunner.Run(typeof(Program).Assembly,
            DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator));
#pragma warning restore CS0618
    }
}
