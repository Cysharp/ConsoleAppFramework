namespace CliFrameworkBenchmarks.Commands;

public class CoconaCommand
{
    public void Execute(
        [Cocona.Option("str", new []{'s'})]
        string? strOption,
        [Cocona.Option("int", new []{'i'})]
        int intOption,
        [Cocona.Option("bool", new []{'b'})]
        bool boolOption)
    {
    }
}
