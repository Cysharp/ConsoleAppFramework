namespace CliFrameworkBenchmarks.Commands;

public class CommandLineParserCommand
{
    [CommandLine.Option('s', "str")]
    public string? StrOption { get; set; }

    [CommandLine.Option('i', "int")]
    public int IntOption { get; set; }

    [CommandLine.Option('b', "bool")]
    public bool BoolOption { get; set; }

    public void Execute()
    {
    }
}
