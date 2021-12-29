
//ConsoleApp.Run(args, (string name) => Console.WriteLine($"Hello {name}"));


args = new[] { "check-timeout" };

var builder = ConsoleApp.CreateBuilder(args);
builder.ConfigureHostOptions(options =>
{
    global::System.Console.WriteLine(options.ShutdownTimeout);
    options.ShutdownTimeout = TimeSpan.FromSeconds(10);
});


var app = builder.Build();


app.AddCommands<DisposeMan>();

app.Run();

// System.CommandLine v2, requires many boilerplate code.
//using System.CommandLine;

//var option = new Option<string>("name");
//var rootCommand = new RootCommand
//{
//    option
//};

//rootCommand.SetHandler((string name) =>
//{
//    Console.WriteLine($"Hello {name}");
//}, option);

//rootCommand.Invoke(args);




//return;

public class DisposeMan : ConsoleAppBase, IDisposable
{
    public void Tako()
    {
        Console.WriteLine("Tako");
    }

    public async Task CheckTimeout()
    {
        await Task.Delay(TimeSpan.FromSeconds(30));
    }

    public void Dispose()
    {
        Console.WriteLine("foo!");
    }
}


//rootCommand.Handler = CommandHandler.Create<int, bool, FileInfo>((intOption, boolOption, fileOption) =>
//{
//    Console.WriteLine($"The value for --int-option is: {intOption}");
//    Console.WriteLine($"The value for --bool-option is: {boolOption}");
//    Console.WriteLine($"The value for --file-option is: {fileOption?.FullName ?? "null"}");
//});

//// Parse the incoming args and invoke the handler
//return rootCommand.InvokeAsync(args).Result;



//using ConsoleAppFramework;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//args = new[] { "iroiro-case", "tako", "--help" };
////args = new[] { "console-foo", "hello-anonymous", "--help" };
////args = new[] { "console-foo", "hello-anonymous", "--hyper-name", "takoyaki" };

//var app = ConsoleApp.Create(args);

////app.AddCommand("foo", (ConsoleAppContext context, [Option(0)] int x, ILogger<string> oreLogger, [Option(1)] int y) =>
////{
////    global::System.Console.WriteLine(context.Timestamp);
////    global::System.Console.WriteLine(x + ":" + y);
////});
//app.AddRoutedCommands();
//app.AddDefaultCommand((string foo) => { });

////app.AddCommands<ConsoleFoo>();

//app.Run();

//static class Foo
//{
//    public static void Barrier(int x, int y)
//    {
//        Console.WriteLine($"OK:{x + y}");
//    }
//}

//public class ConsoleFoo : ConsoleAppBase
//{
//    // [DefaultCommand]
//    public void HelloAnonymous(string hyperName)
//    {
//        Console.WriteLine("OK:" + hyperName);
//    }

//    public void Hello2([Option("n", "name of send user.")] string name, [Option("r", "repeat count.")] int repeat = 3)
//    {
//    }


//}

//public class IroiroCase : ConsoleAppBase
//{
//    [Command(new[] { "tako", "Yaki", "nanobee", "hatchi" })]
//    public void Tes(int I, int i, int ID, int XML, int Xml, int Id, int IdCheck, int IDCheck, int IdCheckZ, int IdCheckXML, int IdCheckXml)
//    {
//    }


//    public void Hot()
//    {
//    }
//}