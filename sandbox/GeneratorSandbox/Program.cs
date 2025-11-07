using ConsoleAppFramework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();

        app.Add("", (CancellationToken cancellationToken, ConsoleAppContext ctx) => { });

        app.Run(args, CancellationToken.None);

    }
}

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
