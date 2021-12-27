using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

args = new[] { "foo", "10", "199" };
//args = new[] { "consolefoo", "tako" };

var app = ConsoleApp.CreateBuilder(args)
    .ConfigureLogging(x =>
    {
        x.ReplaceToSimpleConsole();
    })
    .Build();

app.AddCommand("foo", (ConsoleAppContext context, [Option(0)] int x, ILogger<string> oreLogger, [Option(1)] int y) =>
{
    global::System.Console.WriteLine(context.Timestamp);
    global::System.Console.WriteLine(x + ":" + y);
});
app.AddRoutedCommands();

app.Run();

static class Foo
{
    public static void Barrier(int x, int y)
    {
        Console.WriteLine($"OK:{x + y}");
    }
}

public class ConsoleFoo : ConsoleAppBase
{
    public void Tako()
    {
        Console.WriteLine($"OK");
    }
}