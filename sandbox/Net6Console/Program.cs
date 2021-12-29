using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

args = new[] { "iroiro-case", "tako", "--help" };
//args = new[] { "console-foo", "hello-anonymous", "--help" };
//args = new[] { "console-foo", "hello-anonymous", "--hyper-name", "takoyaki" };

var app = ConsoleApp.Create(args);

//app.AddCommand("foo", (ConsoleAppContext context, [Option(0)] int x, ILogger<string> oreLogger, [Option(1)] int y) =>
//{
//    global::System.Console.WriteLine(context.Timestamp);
//    global::System.Console.WriteLine(x + ":" + y);
//});
app.AddRoutedCommands();
app.AddDefaultCommand((string foo) => { });

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
    // [DefaultCommand]
    public void HelloAnonymous(string hyperName)
    {
        Console.WriteLine("OK:" + hyperName);
    }

    public void Hello2([Option("n", "name of send user.")] string name, [Option("r", "repeat count.")] int repeat = 3)
    {
    }


}

public class IroiroCase : ConsoleAppBase
{
    [Command(new[] { "tako", "Yaki", "nanobee", "hatchi" })]
    public void Tes(int I, int i, int ID, int XML, int Xml, int Id, int IdCheck, int IDCheck, int IdCheckZ, int IdCheckXML, int IdCheckXml)
    {
    }


    public void Hot()
    {
    }
}