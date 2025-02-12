using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace Cocona.Benchmark.External.Commands;

public class SystemCommandLineCommand
{
    public static int ExecuteHandler(string s, int i, bool b) => 0;

    public static int Execute(string[] args)
    {
        var command = new RootCommand
        {
            new Option<string?>(new[] {"--str", "-s"}),
            new Option<int>(new[] {"--int", "-i"}),
            new Option<bool>(new[] {"--bool", "-b"}),
        };

        command.Handler = CommandHandler.Create(ExecuteHandler);
        return command.Invoke(args);
    }

    public static Task<int> ExecuteAsync(string[] args)
    {
        var command = new RootCommand
        {
            new Option<string?>(new[] {"--str", "-s"}),
            new Option<int>(new[] {"--int", "-i"}),
            new Option<bool>(new[] {"--bool", "-b"}),
        };

        command.Handler = CommandHandler.Create(ExecuteHandler);
        return command.InvokeAsync(args);
    }
}
