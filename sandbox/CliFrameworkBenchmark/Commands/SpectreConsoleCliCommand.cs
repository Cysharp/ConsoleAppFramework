using Spectre.Console.Cli;
using System.ComponentModel;

namespace Cocona.Benchmark.External.Commands;

public class SpectreConsoleCliCommand : Command<SpectreConsoleCliCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-s")]
        public string? strOption { get; init; }

        [CommandOption("-i")]
        public int intOption { get; init; }

        [CommandOption("-b")]
        public bool boolOption { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        return 0;
    }
}
