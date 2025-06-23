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
            new Option<string?>("--str", ["-s"]),
            new Option<int>("--int", ["-i"]),
            new Option<bool>("--bool", ["-b"]),
        };

        command.SetAction(parseResult =>
        {
            var handler = CommandHandler.Create(ExecuteHandler);
            return handler.InvokeAsync(parseResult);
        });

        ParseResult parseResult = command.Parse(args);
        return parseResult.Invoke();
    }

    public static Task<int> ExecuteAsync(string[] args)
    {
        var command = new RootCommand
        {
            new Option<string?>("--str", ["-s"]),
            new Option<int>("--int", ["-i"]),
            new Option<bool>("--bool", ["-b"]),
        };

        command.SetAction((parseResult, cancellationToken) =>
        {
            var handler = CommandHandler.Create(ExecuteHandler);
            return handler.InvokeAsync(parseResult);
        });

        ParseResult parseResult = command.Parse(args);
        return parseResult.InvokeAsync();
    }
}
