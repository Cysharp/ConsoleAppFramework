ConsoleAppFramework
===
[![GitHub Actions](https://github.com/Cysharp/ConsoleAppFramework/workflows/Build-Debug/badge.svg)](https://github.com/Cysharp/ConsoleAppFramework/actions) [![Releases](https://img.shields.io/github/release/Cysharp/ConsoleAppFramework.svg)](https://github.com/Cysharp/ConsoleAppFramework/releases)

ConsoleAppFramework is an infrastructure of creating CLI(Command-line interface) tools, daemon, and multi batch application. You can create full feature of command line tool on only one-line.

![image](https://user-images.githubusercontent.com/46207/147662718-f7756523-67a9-4295-b090-3cfc94203017.png)

This simplicity is by C# 10.0 and .NET 6 new features, similar as [ASP.NET Core 6.0 Minimal APIs](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis).

Most minimal API is one-line(with top-level-statements, global-usings).

```csharp
ConsoleApp.Run(args, (string name) => Console.WriteLine($"Hello {name}"));
```

Of course, ConsoleAppFramework has extensibility.

```csharp
// Register two commands(use short-name, argument)
// hello -m
// sum [x] [y]
var app = ConsoleApp.Create(args);
app.AddCommand("hello", ([Option("m", "Message to display.")] string message) => Console.WriteLine($"Hello {message}"));
app.AddCommand("sum", ([Option(0)] int x, [Option(1)] int y) => Console.WriteLine(x + y));
app.Run();
```

You can register public method as command. This provides a simple way to registering multiple commands.

```csharp
// AddCommands register as command.
// echo --msg --repeat(default = 3)
// sum [x] [y]
var app = ConsoleApp.Create(args);
app.AddCommands<Foo>();
app.Run();

public class Foo : ConsoleAppBase
{
    public void Echo(string msg, int repeat = 3)
    {
        for (var i = 0; i < repeat; i++)
        {
            Console.WriteLine(msg);
        }
    }

    public void Sum([Option(0)]int x, [Option(1)]int y)
    {
        Console.WriteLine((x + y).ToString());
    }
}
```

If you have many commands, you can define class separetely and use `AddAllCommandType` to register all commands one-line.

```csharp
// Register `Foo` and `Bar` as SubCommands(You can also use AddSubCommands<T> to register manually).
// foo echo --msg
// foo sum [x] [y]
// bar hello2
var app = ConsoleApp.Create(args);
app.AddAllCommandType();
app.Run();

public class Foo : ConsoleAppBase
{
    public void Echo(string msg)
    {
        Console.WriteLine(msg);
    }

    public void Sum([Option(0)]int x, [Option(1)]int y)
    {
        Console.WriteLine((x + y).ToString());
    }
}

public class Bar : ConsoleAppBase
{
    public void Hello2()
    {
        Console.WriteLine("H E L L O");
    }
}
```

 ConsoleAppFramework is built on [.NET Generic Host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host), you can use configuration, logging, DI, lifetime management by Microsoft.Extensions packages. ConsoleAppFramework do parameter binding from string args, routing many commands, dotnet style help builder, etc.

![image](https://user-images.githubusercontent.com/46207/72047323-a08e0c80-32fd-11ea-850a-7f926adf3d22.png)

Here is the full-sample of power of ConsoleAppFramework.

```csharp
// You can use full feature of Generic Host(same as ASP.NET Core).

var builder = ConsoleApp.CreateBuilder(args);
builder.ConfigureServices((ctx,services) =>
{
    // Register EntityFramework database context
    services.AddDbContext<MyDbContext>();

    // Register appconfig.json to IOption<MyConfig>
    services.Configure<MyConfig>(ctx.Configuration);

    // Using Cysharp/ZLogger for logging to file
    services.AddLogging(logging =>
    {
        logging.AddZLoggerFile("log.txt");
    });
});

var app = builder.Build();

// setup many command, async, short-name/description option, subcommand, DI
app.AddCommand("calc-sum", (int x, int y) => Console.WriteLine(x + y));
app.AddCommand("sleep", async ([Option("t", "seconds of sleep time.")] int time) =>
{
    await Task.Delay(TimeSpan.FromSeconds(time));
});
app.AddSubCommand("verb", "childverb", () => Console.WriteLine("called via 'verb childverb'"));

// You can insert all public methods as sub command => db select / db insert
// or AddCommand<T>() all public methods as command => select / insert
app.AddSubCommands<DatabaseApp>();

// some argument from DI.
app.AddRootCommand((ConsoleAppContext ctx, IOptions<MyConfig> config, string name) => { });

app.Run();

// ----

[Command("db")]
public class DatabaseApp : ConsoleAppBase, IAsyncDisposable
{
    readonly ILogger<DatabaseApp> logger;
    readonly MyDbContext dbContext;
    readonly IOptions<MyConfig> config;

    // you can get DI parameters.
    public DatabaseApp(ILogger<DatabaseApp> logger,IOptions<MyConfig> config, MyDbContext dbContext)
    {
        this.logger = logger;
        this.dbContext = dbContext;
        this.config = config;
    }

    [Command("select")]
    public async Task QueryAsync(int id)
    {
        // select * from...
    }

    // also allow defaultValue.
    [Command("insert")]
    public async Task InsertAsync(string value, int id = 0)
    {
        // insert into...
    }

    // support cleanup(IDisposable/IAsyncDisposable)
    public async ValueTask DisposeAsync()
    {
        await dbContext.DisposeAsync();
    }
}

public class MyConfig
{
    public string FooValue { get; set; } = default!;
    public string BarValue { get; set; } = default!;
}
```

ConsoleAppFramework can create easily to many command application. Also enable to use GenericHost configuration is best way to share configuration/workflow when creating batch application for other .NET web app. If tool is for CI, git pull and run by `dotnet run -- [Command] [Option]` is very helpful.

dotnet's standard CommandLine api - [System.CommandLine](https://github.com/dotnet/command-line-api) is low level, require many boilerplate codes. ConsoleAppFramework is like ASP.NET Core in CLI Applications, no needs boilerplate. However, with the power of Generic Host, it is simple and easy, but much more powerful.

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table of Contents

- [Getting Started](#getting-started)
- [ConsoleApp / ConsoleAppBuilder](#consoleapp--consoleappbuilder)
- [Delegate convention](#delegate-convention)
- [AddCommand](#addcommand)
  - [`AddRootCommand`](#addrootcommand)
  - [`AddCommand` / `AddCommands<T>`](#addcommand--addcommandst)
  - [`AddSubCommand` / `AddSubCommands<T>`](#addsubcommand--addsubcommandst)
  - [`AddAllCommandType`](#addallcommandtype)
- [Complex Argument](#complex-argument)
- [Exit Code](#exit-code)
- [Implicit Using](#implicit-using)
- [CommandAttribute](#commandattribute)
- [OptionAttribute](#optionattribute)
- [Command parameters validation](#command-parameters-validation)
- [Daemon](#daemon)
- [Abort Timeout](#abort-timeout)
- [Filter](#filter)
- [Logging](#logging)
- [Configuration](#configuration)
- [DI](#di)
- [Cleanup](#cleanup)
- [ConsoleAppContext](#consoleappcontext)
- [ConsoleAppOptions](#consoleappoptions)
- [Terminate handling in Console.Read](#terminate-handling-in-consoleread)
- [Publish to executable file](#publish-to-executable-file)
- [v3 Legacy Compatibility](#v3-legacy-compatibility)
- [License](#license)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->


Getting Started
--
NuGet: [ConsoleAppFramework](https://www.nuget.org/packages/ConsoleAppFramework)

```
Install-Package ConsoleAppFramework
```

If you are using .NET 6, automatically enabled implicit global `using ConsoleAppFramework;`. So you can write one line code.

```csharp
ConsoleApp.Run(args, (string name) => Console.WriteLine($"Hello {name}"));
```

You can execute command like `sampletool --name "foo"`.

The Option parser is no longer needed. You can also use the `OptionAttribute` to describe the parameter and set short-name.

```csharp
ConsoleApp.Run(args, ([Option("n", "name of send user.")] string name) => Console.WriteLine($"Hello {name}"));
```

```
Usage: sampletool [options...]

Options:
  -n, --name <String>    name of user. (Required)

Commands:
  help       Display help.
  version    Display version.
```

Method parameter will be required parameter, optional parameter will be oprional parameter with default value. Also support boolean flag, if parameter is bool, in default it will be optional parameter and with `--foo` set true to parameter.

```csharp
// lambda expression does not support default value so require to use local function
static void Hello([Option("m")]string message, [Option("e")] bool end, [Option("r")] int repeat = 3)
{
    for (int i = 0; i < repeat; i++)
    {
        Console.WriteLine(message);
    }
    if (end)
    {
        Console.WriteLine("END");
    }
}

ConsoleApp.Run(args, Hello);
```

```csharp
Options:
  -m, --message <String>     (Required)
  -e, --end                  (Optional)
  -r, --repeat <Int32>       (Default: 3)
```

`help` command (or no argument to pass) and `version` command is enabled in default(You can disable this in options or can override by add same name of command). Also enables `command --help` option. This help format is similar as `dotnet` command, `version` command shows `AssemblyInformationalVersion` or `AssemblylVersion`.

```
> sampletool help
Usage: sampletool [options...]

Options:
  -n, --name <String>     name of user. (Required)
  -r, --repeat <Int32>    repeat count. (Default: 3)

Commands:
  help          Display help.
  version       Display version.
```

```
> sampletool version
1.0.0
```

You can use `Run<T>` or `AddCommands<T>` to add multi commands easily.

```csharp
ConsoleApp.Run<MyCommands>(args);

// require to inherit ConsoleAppBase
public class MyCommands : ConsoleAppBase
{
    //  You can receive DI services in constructor.

    // All public methods is registred.

    // Using [RootCommand] attribute will be root-command
    [RootCommand]
    public void Hello(
        [Option("n", "name of send user.")] string name,
        [Option("r", "repeat count.")] int repeat = 3)
    {
        for (int i = 0; i < repeat; i++)
        {
            Console.WriteLine($"Hello My ConsoleApp from {name}");
        }
    }

    // [Option(int)] describes that parameter is passed by index
    [Command("escape")]
    public void UrlEscape([Option(0)] string input)
    {
        Console.WriteLine(Uri.EscapeDataString(input));
    }

    // define async method returns Task
    [Command("timer")]
    public async Task Timer([Option(0)] uint waitSeconds)
    {
        Console.WriteLine(waitSeconds + " seconds");
        while (waitSeconds != 0)
        {
            // ConsoleAppFramework does not stop immediately on terminate command(Ctrl+C)
            // for allows gracefully shutdown(keeping safe cleanup)
            // so you should pass Context.CancellationToken to async method.
            // If not, abort timeout by HostOptions.ShutdownTimeout(default is 00:00:05).
            await Task.Delay(TimeSpan.FromSeconds(1), Context.CancellationToken);
            waitSeconds--;
            Console.WriteLine(waitSeconds + " seconds");
        }
    }
}
```

You can call like

```
sampletool -n "foo" -r 3
sampletool escape http://foo.bar/
sampletool timer 10
```

This is recommended way to register multi commands.

If you omit `[Command]` attribute, command and option name is used by there name and convert to `kebab-case` in default.

```csharp

// Command is url-escape
// Option  is --input-file
public void UrlEscape(string inputFile)
{
}
```

This converting behaviour can configure by `ConsoleAppOptions.NameConverter`.

ConsoleApp / ConsoleAppBuilder
---
`ConsoleApp` is an entrypoint of creating ConsoleAppFramework app. It has three APIs, `Create`, `CreateBuilder`, `CreateFromHostBuilder` and `Run`.

```csharp
// Create is shorthand of CraeteBuilder(args).Build();
var app = ConsoleApp.Create(args);

// Builder returns IHost so you can configure application hosting option.
var app = ConsoleApp.CreateBuilder(args)
    .ConfigureServices(services =>
    {
    })
    .Build();

// Run is shorthand of Create(args).AddRootCommand(rootCommand).Run();
// If you want to create simple app, this API is faster.
ConsoleApp.Run(args, /* lambda expression */);

// Run<T> is shorthand of Create(args).AddCommands<T>().Run();
// AddCommands<T> is recommend option to register many commands.
ConsoleApp.Run<MyCommands>(args);
```

When calling `Create/CreateBuilder/CreateFromHostBuilder`, also configure `ConsoleAppOptions`. Full option details, see [ConsoleAppOptions](#consoleappoptions) section.

```csharp
var app = ConsoleApp.Create(args, options =>
{
    options.ShowDefaultCommand = false;
    options.NameConverter = x => x.ToLower();
});
```

Advanced API of `ConsoleApp`, `CreateFromHostBuilder` creates ConsoleApp from IHostBuilder.

```csharp
// Setup services outside of ConsoleAppFramework.
var hostBuilder = Host.CreateDefaultBuilder()
    .ConfigureServices();
    
var app = ConsoleApp.CreateFromHostBuilder(hostBuilder);
```

`ConsoleAppBuilder` itself is `IHostBuilder` so you can use any configuration methods like `ConfigureServices`, `ConfigureLogging`, etc. If method chain is not returns `ConsoleAppBuilder`(for example,  using external lib's extension methods), can not get `ConsoleApp` directly. In that case, use `BuildAsConsoleApp()` instead of `Build()`.

`ConsoleApp` exposes some utility properties.

* `IHost` Host
* `ILogger<ConsoleApp>` Logger
* `IServiceProvider` Services
* `IConfiguration` Configuration
* `IHostEnvironment` Environment
* `IHostApplicationLifetime` Lifetime

`Run()` and `RunAsync(CancellationToken)` to finally invoke application. Run is shorthand of `RunAsync().GetAwaiter().GetResult()` so receives same result of `await RunAsync()`. On Entrypoint, there is not much need to do `await RunAsync()`. Therefore, it is usually a good to choose `Run()`.

Delegate convention
---
`AddCommand` accepts `Delegate` in argument. In C# 10.0 allows naturaly syntax of lambda expressions.

```csharp
app.AddCommand("no-argument", () => { });
app.AddCommand("any-arguments",  (int x, string y, TimeSpan z) => { });
app.AddCommand("instance", new MyClass().Cmd);
app.AddCommand("async", async () => { });
app.AddCommand("attribute", ([Option("msg")]string message) => { });

static void Hello1() { }
app.AddCommand("local-static", Hello1);

void Hello2() { }
app.AddCommand("local-method", Hello2);

async Task Async() { }
app.AddCommand("async-method", Async);

void OptionalParameter(int x = 10, int y = 20) { }
app.AddCommand("optional", OptionalParameter);

public class MyClass
{
    public void Cmd()
    {
        Console.WriteLine("OK");
    }
}
```

lambda expressions can not use optional parameter so if you want to need it, using local/static functions.

Delegate(both lambda and method) allows to receive `ConsoleAppContext` or any your DI types. DI types is ignored as parameter.

```csharp
// option is --param1, --param2
app.AddCommand("di", (ConsoleAppContext ctx, ILogger logger, int param1, int param2) => { });
```

AddCommand
---
### `AddRootCommand`

`RootCommand` means default(no command name) command of application. `ConsoleApp.Run(Delegate)` uses root command.

### `AddCommand` / `AddCommands<T>`

`AddCommand` requires first argument as command-name. `AddCommands<T>` allows to register many command via `ConsoleAppBase` `ConsoleAppBase` has `Context`, it has executing information and `CancellationToken`.

```csharp
// Commands:
//   hello
//   world
app.AddCommands<MyCommands>();
app.Run();

// Inherit ConsoleAPpBase
public class MyCommands : ConsoleAppBase, IDisposable
{
    readonly ILogger<MyCommands> logger;

    //  You can receive DI services in constructor.
    public MyCommands(ILogger<MyCommands> logger)
    {
        this.logger = logger;
    }

    // All public methods is registered.

    // Using [RootCommand] attribute will be root-command
    [RootCommand]
    public void Hello() 
    {
        // Context has any useful information.
        Console.WriteLine(this.Context.Timestamp);
    }

    public async Task World() 
    {
        await Task.Delay(1000, this.Context.CancellationToken);
    }

    // If implements IDisposable, called for cleanup
    public void Dispose()
    {
    }
}
```

### `AddSubCommand` / `AddSubCommands<T>`

`AddSubCommand(string parentCommandName, string commandName, Delegate command)` registers nested command.

```csharp
// Commands:
//   foo bar1
//   foo bar2
//   foo bar3
app.AddSubCommand("foo", "bar1", () => { });
app.AddSubCommand("foo", "bar2", () => { });
app.AddSubCommand("foo", "bar3", () => { });
```

`AddSubCommands<T>` is similar as `AddCommands<T>` but used type-name(or `[Command]` name) as parentCommandName.

```csharp
// Commands:
//   my-commands hello
//   my-commands world
app.AddSubCommands<MyCommands>();
```

### `AddAllCommandType`

`AddAllCommandType` searches all `ConsoleAppBase` type in assembly and register by `AddSubCommands<T>`.

```csharp
// Commands:
//   foo echo
//   foo sum
//   bar hello2
app.AddAllCommandType();

// Batches.
public class Foo : ConsoleAppBase
{
    public void Echo(string msg)
    {
        Console.WriteLine(msg);
    }

    public void Sum(int x, int y)
    {
        Console.WriteLine((x + y).ToString());
    }
}

public class Bar : ConsoleAppBase
{
    public void Hello2()
    {
        Console.WriteLine("H E L L O");
    }
}
```

This is most easy to create many commands so useful for application batch that requires many many command. 

Commands are searched from loaded assemblies(in default `AppDomain.CurrentDomain.GetAssemblies()`), when does not touch other assemblies type, it will be trimmed and can not load it. In that case, use `AddAllCommandType(params Assembly[] searchAssemblies)` overload to pass target assembly, for example `AddAllCommandType(typeof(Foo).Assembly)` preserve types.

Complex Argument
---
If the argument is not primitive, you can pass JSON string.

```csharp
public class ComplexArgTest : ConsoleAppBase
{
    public void Foo(int[] array, Person person)
    {
        Console.WriteLine(string.Join(", ", array));
        Console.WriteLine(person.Age + ":" + person.Name);
    }
}

public class Person
{
    public int Age { get; set; }
    public string Name { get; set; }
}
```

You can call like here.

```
> sampletool -array [10,20,30] -person {"Age":10,"Name":"foo"}

# including space, use escaping
> SampleApp.exe -array [10,20,30] -person "{\"Age\":10,\"Name\":\"foo bar\"}"
```

> be careful with JSON string double quotation.

For the array handling, it can be a treat without correct JSON.
e.g. one-length argument can handle without `[]`.

```csharp
Foo(int[] array)
> SampleApp.exe -array 9999
```

multiple-argument can handle by split with ` ` or `,`.

```csharp
Foo(int[] array)
> SampleApp.exe -array "11 22 33"
> SampleApp.exe -array "11,22,33"
> SampleApp.exe -array "[11,22,33]"
```

string argument can handle without `"`.

```csharp
Foo(string[] array)
> SampleApp.exe -array hello
> SampleApp.exe -array "foo bar baz"
> SampleApp.exe -array foo,bar,baz
> SampleApp.exe -array "["foo","bar","baz"]"
```

Exit Code
---
If the method returns `int` or `Task<int>` or `ValueTask<int> value, ConsoleAppFramework will set the return value to the exit code.

```csharp
public class ExampleApp : ConsoleAppBase
{
    [Command("exit")]
    public int ExitCode()
    {
        return 12345;
    }
    
    [Command("exit-with-task")]
    public async Task<int> ExitCodeWithTask()
    {
        return 54321;
    }
}
```

> **NOTE**: If the method throws an unhandled exception, ConsoleAppFramework always set `1` to the exit code.

Implicit Using
---
In .NET 6, `global using ConsoleAppFramework` is enabled in default. If you remove global using, setup this element to target `.csproj`.

```xml
<ItemGroup>
    <Using Remove="ConsoleAppFramework" />
</ItemGroup>
```

CommandAttribute
---
`CommandAttribute` enables subscommand on `RunConsoleAppFramework<T>()`(for single type CLI app), changes command name on `RunConsoleAppFramework()`(for muilti type command routing), also describes the description.

```csharp
RunConsoleAppFramework<App>();

public class App : ConsoleAppBase
{
    // as Root Command(no command argument)
    public void Run()
    {
    }

    [Command("sec", "sub comman of this app")]
    public void Second()
    {
    }
}
```

```csharp
RunConsoleAppFramework();

public class App2 : ConsoleAppBase
{
    // routing command: `app2 exec`
    [Command("exec", "exec app.")]
    public void Exec1()
    {
    }
}

// command attribute also can use to class.
[Command("mycmd")]
public class App3 : ConsoleAppBase
{
     // routing command: `mycmd e2`
    [Command("e2", "exec app 2.")]
    public void ExecExec()
    {
    }
}
```

OptionAttribute
---
OptionAttribute configure parameter, it can set shortName or order index, and help description.

If you want to add only description, set "" or null to shortName parameter.

```csharp
public void Hello(
    [Option("n", "name of send user.")]string name,
    [Option("r", "repeat count.")]int repeat = 3)
{
}

[Command("escape")]
public void UrlEscape([Option(0, "input of this command")]string input)
{
}

[Command("unescape")]
public void UrlUnescape([Option(null, "input of this command")]string input)
{
}
```

## Command parameters validation

Values of command parameters can be validated via validation attributes from `System.ComponentModel.DataAnnotations`
namespace and custom ones inheriting `ValidationAttribute` type.

```csharp
using System.ComponentModel.DataAnnotations;
// ...

internal class TestConsoleApp : ConsoleAppBase
{
    [Command("some-command")]
    public void SomeCommand(
        [EmailAddress] string firstArg,
        [Range(0, 2)]  int secondArg) => Console.WriteLine($"hello from {nameof(TestConsoleApp)}");
}
```

Output (command invoked with params [**--first-arg "invalid-email-address" --second-arg" 10**])

```
Some parameters have invalid values:
first-arg (invalid-email-address): The String field is not a valid e-mail address.
second-arg (10): The field Int32 must be between 0 and 2.
```

Daemon
---
If use infinite-loop, it becomes daemon program. `ConsoleAppContext.CancellationToken` is lifecycle token of application. You can check `CancellationToken.IsCancellationRequested` and shutdown gracefully. 

```csharp
public class Daemon : ConsoleAppBase
{
    [RootCommand]
    public async Task Run()
    {
        // you can write infinite-loop while stop request(Ctrl+C or docker terminate).
        try
        {
            while (!this.Context.CancellationToken.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine("Wait One Minutes");
                }
                catch (Exception ex)
                {
                    // error occured but continue to run(or terminate).
                    Console.WriteLine(ex, "Found error");
                }

                // wait for next time
                await Task.Delay(TimeSpan.FromMinutes(1), this.Context.CancellationToken);
            }
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            // you can write finally exception handling(without cancellation)
        }
        finally
        {
            // you can write cleanup code here.
        }
    }
}
```

Abort Timeout
---
ConsoleAppFramework's execution lifetime is managed via generic host. If you do cancel(Ctrl+C), host starts cancellation process with timeout. If you don't pass `CancellationToken` in async method, does not cancel immediately.

```csharp
public async Task Wait1() 
{
    // Not good.
    await Task.Delay(TimeSpan.FromMinutes(60));
}

public async Task Wait2() 
{
    // Good.
    await Task.Delay(TimeSpan.FromMinutes(60), this.Context.CancellationToken);
}
```

Default timeout time is `00:00:05`, you can change via `ConfigureHostOptions`.

```csharp
var app = ConsoleApp.CreateBuilder(args)
    .ConfigureHostOptions(options =>
    {
        // change timeout.
        options.ShutdownTimeout = TimeSpan.FromMinutes(30);
    })
    .Build();
```

Filter
---
Filter can hook before/after batch running event. You can implement `ConsoleAppFilter` for it and attach to global/class/method.

```csharp
public class MyFilter : ConsoleAppFilter
{
    // Filter is instantiated by DI so you can get parameter by constructor injection.

    public async override ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next)
    {
        try
        {
            /* on before */
            await next(context); // next
        }
        catch
        {
            /* on after */
            throw;
        }
        finally
        {
            /* on finally */
        }
    }
}
```

`ConsoleAppContext.Timestamp` has start time so if subtraction from now, get elapsed time.

```csharp
public class LogRunningTimeFilter : ConsoleAppFilter
{
    public override async ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next)
    {
        context.Logger.LogInformation("Call method at " + context.Timestamp.ToLocalTime()); // LocalTime for human readable time
        try
        {
            await next(context);
            context.Logger.LogInformation("Call method Completed successfully, Elapsed:" + (DateTimeOffset.UtcNow - context.Timestamp));
        }
        catch
        {
            context.Logger.LogInformation("Call method Completed Failed, Elapsed:" + (DateTimeOffset.UtcNow - context.Timestamp));
            throw;
        }
    }
}
```

In default, ConsoleAppFramework does not prevent double startup but if create filter, can do. 

```csharp
public class MutexFilter : ConsoleAppFilter
{
    public override async ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next)
    {
        var name = context.MethodInfo.DeclaringType.Name + "." + context.MethodInfo.Name;
        using (var mutex = new Mutex(true, name, out var createdNew))
        {
            if (!createdNew)
            {
                throw new Exception($"already running {name} in another process.");
            }
            
            await next(context);
        }
    }
}
```

There filters can pass to `ConsoleAppOptions.GlobalFilters` on startup or attach by attribute on class, method.

```csharp
var app = ConsoleApp.Create(args, options =>
{
    options.GlobalFilters = new ConsoleAppFilter[]
    {
        new MutextFilter() { Order = -9999 } ,
        new LogRunningTimeFilter() { Oder = -9998 }, 
    }
});

[ConsoleAppFilter(typeof(MyFilter3))]
public class MyBatch : ConsoleAppBase
{
    [ConsoleAppFilter(typeof(MyFilter4), Order = -9999)]
    [ConsoleAppFilter(typeof(MyFilter5), Order = 9999)]
    public void Do()
    {
    }
}
```

Execution order can control by `int Order` property.

Logging
---
In default, `Context.Logger` has `ILogger<ConsoleApp>` and `ILogger<T>` can inject to constructor. Default `ConsoleLogger` format in `Host.CreateDefaultBuilder` is supernumerary and not suitable for console application. ConsoleAppFramework provides `SimpleConsoleLogger` to replace default ConsoleLogger in default. If you want to keep default `ConsoleLogger`, use `ConsoleAppOptions.ReplaceToUseSimpleConsoleLogger` to `false`.

If you want to use high performance logger/output to file, also use [Cysharp/ZLogger](https://github.com/Cysharp/ZLogger/) that easy to integrate ConsoleAppFramework.

```csharp
using ZLogger;

var app = ConsoleApp.CreateDefaultBuilder(args)
    .ConfigureLogging(x =>
    {
        x.ClearProviders(); // clear all providers
        x.SetMinimumLevel(LogLevel.Trace); // change log level if you want

        x.AddZLoggerConsole(); // add ZLogger Console
        x.AddZLoggerFile("fileName.log"); // add ZLogger file output
    })
    .Build();
```

Configuration
---
ConsoleAppFramework is just an infrastructure. You can add `appsettings.json` or other configs as .NET Core offers via `Microsoft.Extensions.Options`.
You can add `appsettings.json` and `appsettings.{environment}.json` and typesafe load via map config to Class w/IOption.

Here's single contained batch with Config loading sample.

```json
// appconfig.json(Content, Copy to Output Directory)
{
  "Foo": 42,
  "Bar": true
}
```

```csharp
using Microsoft.Extensions.DependencyInjection;

var app = ConsoleApp.CreateBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // mapping config json to IOption<MyConfig>
        // requires "Microsoft.Extensions.Options.ConfigurationExtensions" package
        // if you want to map subscetion in json, use Configure<T>(hostContext.Configuration.GetSection("foo"))
        services.Configure<MyConfig>(hostContext.Configuration);
    })
    .Build();

public class ConfigAppSample : ConsoleAppBase
{
    MyConfig config;

    // get configuration from DI.
    public ConfigAppSample(IOptions<MyConfig> config)
    {
        this.config = config.Value;
    }

    public void ShowOption()
    {
        Console.WriteLine(config.Bar);
        Console.WriteLine(config.Foo);
    }
}
```

for the details, please see [.NET Core Generic Host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host) documentation.

DI
---
You can use DI(constructor injection) by GenericHost.

```csharp
IOptions<MyConfig> config;
ILogger<MyApp> logger;

public MyApp(IOptions<MyConfig> config, ILogger<MyApp> logger)
{
    this.config = config;
    this.logger = logger;
}
```

DI also allows delegate registration.

```csharp
app.AddCommand("di", (ConsoleAppContext ctx, ILogger logger, int param1, int param2) => { });
```

DI also inject to filter.

Cleanup
---
You can implement `IDisposable.Dispose` or `IAsyncDisposable.DisposeAsync` explicitly, that is called after command finished.

```csharp
public class MyApp : ConsoleAppBase, IDisposable
{
    public void Hello()
    {
        Console.WriteLine("Hello");
    }

    // Dispose/DisposeAsync method is not registered as Command.
    public void Dispose()
    {
        Console.WriteLine("DISPOSED");
    }
}
```

If implements both `IDisposable` and `IAsyncDisposable`, called only `IAsyncDisposable`.

```csharp
public class MyApp : ConsoleAppBase, IDisposable, IAsyncDisposable
{
    public void Hello()
    {
        Console.WriteLine("Hello");
    }

    public void Dispose()
    {
        Console.WriteLine("Not called.");
    }
        
    public async ValueTask DisposeAsync()
    {
        Console.WriteLine("called.");
    }
}
```

ConsoleAppContext
---
ConsoleAppContext is injected to property on method executing.

```csharp
public class ConsoleAppContext
{
    public string?[] Arguments { get; }
    public DateTime Timestamp { get; }
    public CancellationToken CancellationToken { get; }
    public ILogger<ConsoleAppEngine> Logger { get; }
    public MethodInfo MethodInfo { get; }
    public IServiceProvider ServiceProvider { get; }
    public IDictionary<string, object> Items { get; }

    public void Cancel();
    public void Terminate();
}
```

`Cancel()` set `CancellationToken` to canceled. Also `Terminate()` set token to cancled and terminate process(internal throws `OperationCanceledException` immediately).

ConsoleAppOptions
---
You can configure framework behaviour by ConsoleAppOptions.

```csharp
var app = ConsoleApp.Create(args, options =>
{
    options.StrictOption = false, // default is true.
    options.ShowDefaultCommand = false, // default is true
});
```

```csharp
public class ConsoleAppOptions
{
    /// <summary>Argument parser uses strict(-short, --long) option. Default is true.</summary>
    public bool StrictOption { get; set; } = true;

    /// <summary>Show default command(help/version) to help. Default is true.</summary>
    public bool ShowDefaultCommand { get; set; } = true;

    public bool ReplaceToUseSimpleConsoleLogger { get; set; } = true;

    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    public ConsoleAppFilter[]? GlobalFilters { get; set; }

    public bool NoAttributeCommandAsImplicitlyDefault { get; set; }

    public Func<string, string> NameConverter { get; set; } = KebabCaseConvert;

    public string? ApplicationName { get; set; } = null;
}
```

If StrictOption = false, does not distinguish between the number of `-`.  For example, this method

```
public void Hello([Option("m", "Message to display.")]string message)
```

can pass argument by `-m`, `--message` and `-message`. This is styled like a go lang command. But if you want to strictly distinguish argument of `-`, set `StrcitOption = true`(default), that allows `-m` and `--message`.

Also, by default, the `help` and `version` commands appear as help, which can be hidden by setting `ShowDefaultCommand = false`.

NameConverter is used type-name, method-name, parameter-name converting as command. Default is convert to lower kebab-case.

```csharp
// my-command query-data --organization-id --user-id
public class MyCommand
{
    public void QueryData(string organizationId, string userId);
}
```

You can set func to change this behaviour like `NameConverter = x => x.ToLower();`.

`ApplicationName` configure help usages `Usage: ***`, default(null) shows filename without extension.

Terminate handling in Console.Read
---
ConsoleAppFramework handle terminate signal(Ctrl+C) gracefully with `ConsoleAppContext.CancellationToken`. If your application waiting with Console.Read/ReadLine/ReadKey, requires additional handling.

```csharp
// case of Console.Read/ReadLine, pressed Ctrl+C, Read returns null.
ConsoleApp.Run(args, (ConsoleAppContext ctx) =>
{
    var read = Console.ReadLine();
    if (read == null) ctx.Terminate();
});
```

```csharp
// case of Console.ReadKey, can not cancel itself so use with Task.Run and WaitAsync.
ConsoleApp.Run(args, async (ConsoleAppContext ctx) =>
{
    var key = await Task.Run(() => Console.ReadKey()).WaitAsync(ctx.CancellationToken);
});
```

Publish to executable file
---
[dotnet run](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-run) is useful for local development or execute in CI tool. For example in CI, git pull and execute by `dotnet run -- --options` is easy to manage and execute utilities.

[dotnet publish](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish) to create executable file. [.NET Core 3.0 offers Single Executable File](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-core-3-0) via `PublishSingleFile`.

CLI tool can use [.NET Core Local/Global Tools](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools). If you want to create it, check the [Tutorial: Create a .NET tool using the .NET CLI](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-create) and [Use a global tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-use) or [Use a local tool](https://docs.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use).

v3 Legacy Compatibility
---
v1-v3 does not exist minimal api style(`ConsoleApp.Create/CreateBuilder`).

```csharp
await Host.CreateDefaultBuilder()
    .RunConsoleAppFrameworkAsync<Program>(args);
```

`RunConsoleAppFrameworkAsync` is still exists but does not recommend to use. Also, since v4, there is a change in the default behavior. When `RunConsoleAppFrameworkAsync` is used, the option settings of v3 and earlier will be used.

```csharp
options.NoAttributeCommandAsImplicitlyDefault = true;
options.StrictOption = false;
options.NameConverter = x => x.ToLower();
options.ReplaceToUseSimpleConsoleLogger = false;
```

You can also get this option setting in `ConsoleAppOptions.CreateLegacyCompatible()`.

License
---
This library is under the MIT License.
