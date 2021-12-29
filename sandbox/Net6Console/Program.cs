
using ConsoleAppFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net6Console;
using ZLogger;


ConsoleApp.Run(args, (string name) => Console.WriteLine($"Hello {name}"));

//args = new[] { "--message", "tako" };



//static int Hello([Option("m")]string message, [Option("e")] bool end, [Option("r")] int repeat = 3)
//{
//    for (int i = 0; i < repeat; i++)
//    {
//        Console.WriteLine(message);
//    }
//    if (end)
//    {
//        Console.WriteLine("END");
//    }
//    //throw new OperationCanceledException("hogemoge");
//    return 999;
//}

//ConsoleApp.Run(args, Hello);

//return;


//args = new[] { "help" };



//// You can use full feature of Generic Host(same as ASP.NET Core).

//var builder = ConsoleApp.CreateBuilder(args);
//builder.ConfigureServices((ctx, services) =>
//{
//    // Register EntityFramework database context
//    services.AddDbContext<MyDbContext>();

//    // Register appconfig.json to IOption<MyConfig>
//    services.Configure<MyConfig>(ctx.Configuration);

//    // Using Cysharp/ZLogger for logging to file
//    services.AddLogging(logging =>
//    {
//        logging.AddZLoggerFile("log.txt");
//    });
//});


//var app = builder.Build();

//// setup many command, async, short-name/description option, subcommand, DI
//app.AddCommand("calc-sum", (int x, int y) => Console.WriteLine(x + y));
//app.AddCommand("sleep", async ([Option("t", "seconds of sleep time.")] int time) =>
//{
//    await Task.Delay(TimeSpan.FromSeconds(time));
//});
//app.AddSubCommand("verb", "childverb", () => Console.WriteLine("called via 'verb childverb'"));

//// You can insert all public methods as sub command => db select / db insert
//// or AddCommand<T>() all public methods as command => select / insert
//app.AddSubCommands<DatabaseApp>();

//// some argument from DI.
//app.AddRootCommand((ConsoleAppContext ctx, IOptions<MyConfig> config, string name) => { });

//app.Run();

// ----

//[Command("db")]
//public class DatabaseApp : ConsoleAppBase, IAsyncDisposable
//{
//    readonly ILogger<DatabaseApp> logger;
//    readonly MyDbContext dbContext;
//    readonly IOptions<MyConfig> config;

//    // you can get DI parameters.
//    public DatabaseApp(ILogger<DatabaseApp> logger, IOptions<MyConfig> config, MyDbContext dbContext)
//    {
//        this.logger = logger;
//        this.dbContext = dbContext;
//        this.config = config;
//    }

//    [Command("select")]
//    public async Task QueryAsync(int id)
//    {
//        // select * from...
//    }

//    // also allow defaultValue.
//    [Command("insert")]
//    public async Task InsertAsync(string value, int id = 0)
//    {
//        // insert into...
//    }

//    // support cleanup(IDisposable/IAsyncDisposable)
//    public async ValueTask DisposeAsync()
//    {
//        await dbContext.DisposeAsync();
//    }
//}

//public class MyConfig
//{
//    public string FooValue { get; set; } = default!;
//    public string BarValue { get; set; } = default!;
//}


//// System.CommandLine
//// Create a root command with some options
////using System.CommandLine;

////var rootCommand = new RootCommand
////{
////    new Option<int>(
////        "--int-option",
////        getDefaultValue: () => 42,
////        description: "An option whose argument is parsed as an int"),
////    new Option<bool>(
////        "--bool-option",
////        "An option whose argument is parsed as a bool"),
////    new Option<FileInfo>(
////        "--file-option",
////        "An option whose argument is parsed as a FileInfo")
////};

////rootCommand.Description = "My sample app";

////rootCommand.SetHandler<int, bool, FileInfo>((intOption, boolOption, fileOption) =>
////{
////    Console.WriteLine($"The value for --int-option is: {intOption}");
////    Console.WriteLine($"The value for --bool-option is: {boolOption}");
////    Console.WriteLine($"The value for --file-option is: {fileOption?.FullName ?? "null"}");
////});

////await rootCommand.InvokeAsync(args);



////args = new[] { "check-timeout" };

////var builder = ConsoleApp.CreateBuilder(args);
////builder.ConfigureHostOptions(options =>
////{
////    global::System.Console.WriteLine(options.ShutdownTimeout);
////    options.ShutdownTimeout = TimeSpan.FromSeconds(10);
////});


////var app = builder.Build();


////app.AddCommands<DisposeMan>();

////app.Run();

//// System.CommandLine v2, requires many boilerplate code.
////using System.CommandLine;

////var option = new Option<string>("name");
////var rootCommand = new RootCommand
////{
////    option
////};

////rootCommand.SetHandler((string name) =>
////{
////    Console.WriteLine($"Hello {name}");
////}, option);

////rootCommand.Invoke(args);




////return;

//public class DisposeMan : ConsoleAppBase, IDisposable
//{
//    public void Tako()
//    {
//        Console.WriteLine("Tako");
//    }

//    public async Task CheckTimeout()
//    {
//        await Task.Delay(TimeSpan.FromSeconds(30));
//    }

//    public void Dispose()
//    {
//        Console.WriteLine("foo!");
//    }
//}


////rootCommand.Handler = CommandHandler.Create<int, bool, FileInfo>((intOption, boolOption, fileOption) =>
////{
////    Console.WriteLine($"The value for --int-option is: {intOption}");
////    Console.WriteLine($"The value for --bool-option is: {boolOption}");
////    Console.WriteLine($"The value for --file-option is: {fileOption?.FullName ?? "null"}");
////});

////// Parse the incoming args and invoke the handler
////return rootCommand.InvokeAsync(args).Result;



////using ConsoleAppFramework;
////using Microsoft.Extensions.Hosting;
////using Microsoft.Extensions.Logging;

////args = new[] { "iroiro-case", "tako", "--help" };
//////args = new[] { "console-foo", "hello-anonymous", "--help" };
//////args = new[] { "console-foo", "hello-anonymous", "--hyper-name", "takoyaki" };

////var app = ConsoleApp.Create(args);

//////app.AddCommand("foo", (ConsoleAppContext context, [Option(0)] int x, ILogger<string> oreLogger, [Option(1)] int y) =>
//////{
//////    global::System.Console.WriteLine(context.Timestamp);
//////    global::System.Console.WriteLine(x + ":" + y);
//////});
////app.AddRoutedCommands();
////app.AddDefaultCommand((string foo) => { });

//////app.AddCommands<ConsoleFoo>();

////app.Run();

////static class Foo
////{
////    public static void Barrier(int x, int y)
////    {
////        Console.WriteLine($"OK:{x + y}");
////    }
////}

////public class ConsoleFoo : ConsoleAppBase
////{
////    // [DefaultCommand]
////    public void HelloAnonymous(string hyperName)
////    {
////        Console.WriteLine("OK:" + hyperName);
////    }

////    public void Hello2([Option("n", "name of send user.")] string name, [Option("r", "repeat count.")] int repeat = 3)
////    {
////    }


////}

////public class IroiroCase : ConsoleAppBase
////{
////    [Command(new[] { "tako", "Yaki", "nanobee", "hatchi" })]
////    public void Tes(int I, int i, int ID, int XML, int Xml, int Id, int IdCheck, int IDCheck, int IdCheckZ, int IdCheckXML, int IdCheckXml)
////    {
////    }


////    public void Hot()
////    {
////    }
////}