using ConsoleAppFramework;


args = "some-command hello --global-flag flag-value -- more args here".Split(" ");

var app = ConsoleApp.Create();

app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    var flag = builder.AddGlobalOption<string>("--global-flag");
    return new GlobalOptions(flag);
});

app.UseFilter<SomeFilter>();
app.Add<Commands>();
app.Run(args);

internal record GlobalOptions(string Flag);

internal class Commands
{
    [Command("some-command")]
    public void SomeCommand([Argument] string commandArg, ConsoleAppContext context)
    {
        Console.WriteLine($"ARG: {commandArg}");
        Console.WriteLine($"ESCAPED: {string.Join(", ", context.EscapedArguments.ToArray()!)}");
    }
}

internal class SomeFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"FLAG: {((GlobalOptions)context.GlobalOptions!).Flag}");
        await Next.InvokeAsync(context, cancellationToken);
    }
}
