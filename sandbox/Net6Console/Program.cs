using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

args = new[] { "help" };
//args = new[] { "consolefoo", "tako" };

var app = ConsoleApp.CreateBuilder(args)
    .ConfigureLogging(x =>
    {
        x.ReplaceToSimpleConsole();
    })
    .Build();

//app.AddCommand("foo", (ConsoleAppContext context, [Option(0)] int x, ILogger<string> oreLogger, [Option(1)] int y) =>
//{
//    global::System.Console.WriteLine(context.Timestamp);
//    global::System.Console.WriteLine(x + ":" + y);
//});
app.AddRoutedCommands();

//app.AddCommands<ConsoleFoo>();

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
    //[DefaultCommand]
    public void Hello1([Option("n", "name of send user.")] string name, [Option("r", "repeat count.")] int repeat = 3)
    {
    }

    public void Hello2([Option("n", "name of send user.")] string name, [Option("r", "repeat count.")] int repeat = 3)
    {
    }
}

