using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.CommandLine;

namespace Cocona.Benchmark.External.Commands;

public class SystemCommandLineCommand
{
    public int Execute(string[] args)
    {
        var stringOption = new Option<string>("--str", "-s");
        var intOption = new Option<int>("--int", "-i");
        var boolOption = new Option<bool>("--bool", "-b");

        var command = new RootCommand { stringOption, intOption, boolOption };

        command.SetAction(parseResult =>
        {
            _ = parseResult.GetValue(stringOption);
            _ = parseResult.GetValue(intOption);
            _ = parseResult.GetValue(boolOption);
        });

        return command.Parse(args).Invoke();
    }
}
