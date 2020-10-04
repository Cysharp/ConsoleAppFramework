ConsoleAppFramework
===
[![GitHub Actions](https://github.com/Cysharp/ConsoleAppFramework/workflows/Build-Debug/badge.svg)](https://github.com/Cysharp/ConsoleAppFramework/actions) [![Releases](https://img.shields.io/github/release/Cysharp/ConsoleAppFramework.svg)](https://github.com/Cysharp/ConsoleAppFramework/releases)

ConsoleAppFramework is an infrastructure of creating CLI(Command-line interface) tools, daemon, and multi batch application.

![image](https://user-images.githubusercontent.com/46207/72047323-a08e0c80-32fd-11ea-850a-7f926adf3d22.png)

ConsoleAppFramework is built on [.NET Generic Host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host), you can use configuration, logging, DI, lifetime management by Microsoft.Extensions packages. ConsoleAppFramework do parameter binding from string args, routing multi command, dotnet style help builder, etc.

This concept is same as [Laravel Zero](https://laravel-zero.com/) of PHP. Similar competitor is [dotnet/command-line-api](https://github.com/dotnet/command-line-api)'s `System.CommandLine.Hosting` + `System.CommandLine.DragonFruit` but it is preview and currently not productivity.

NuGet: [ConsoleAppFramework](https://www.nuget.org/packages/ConsoleAppFramework)

```
Install-Package ConsoleAppFramework
```

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table of Contents

- [CLI Tools](#cli-tools)
- [Automatically Class/Method command routing](#automatically-classmethod-command-routing)
- [Complex Argument](#complex-argument)
- [Exit Code](#exit-code)
- [CommandAttribute](#commandattribute)
- [OptionAttribute](#optionattribute)
- [Daemon](#daemon)
- [Filter](#filter)
- [Logging](#logging)
- [Configuration](#configuration)
- [DI](#di)
- [ConsoleAppContext](#consoleappcontext)
- [ConsoleAppOptions](#consoleappoptions)
- [Web Interface with Swagger](#web-interface-with-swagger)
- [Publish to executable file](#publish-to-executable-file)
- [License](#license)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

CLI Tools
---

CLI Tools(Console Application) can write by simple method, argument is automatically binded to parameter.

```csharp
using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

// Entrypoint, create from the .NET Core Console App.
class Program : ConsoleAppBase // inherit ConsoleAppBase
{
    static async Task Main(string[] args)
    {
        // target T as ConsoleAppBase.
        await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<Program>(args);
    }

    // allows void/Task return type, parameter is automatically binded from string[] args.
    public void Run(string name, int repeat = 3)
    {
        for (int i = 0; i < repeat; i++)
        {
            Console.WriteLine($"Hello My ConsoleApp from {name}");
        }
    }
}
```

You can execute command like `SampleApp.exe -name "foo" -repeat 5`.

The Option parser is no longer needed. You can also use the `OptionAttribute` to describe the parameter.

```csharp
public void Hello(
    [Option("n", "name of send user.")]string name, 
    [Option("r", "repeat count.")]int repeat = 3)
{
```

Method parameter will be required parameter, optional parameter will be oprional parameter. Also support boolean flag, if parameter is bool, in default it will be optional parameter and with `-foo` set true to parameter.

`help` command (or no argument to pass) shows there detail. This help format is same as `dotnet` command.

```
> SampleApp.exe help
Usage: SampleApp [options...]

Options:
  -n, -name <String>     name of send user. (Required)
  -r, -repeat <Int32>    repeat count. (Default: 3)

Commands:
  help          Display help.
  version       Display version.
```

`version` option shows `AssemblyInformationalVersion` or `AssemblylVersion`.

```
> SampleApp.exe version
1.0.0
```

You can use `CommandAttribute` to create multi command program.

```csharp
class Program : ConsoleAppBase
{
    static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<Program>(args);
    }

    // default(no command)
    public void Hello(
        [Option("n", "name of send user.")]string name,
        [Option("r", "repeat count.")]int repeat = 3)
    {
        for (int i = 0; i < repeat; i++)
        {
            Console.WriteLine($"Hello My ConsoleApp from {name}");
        }
    }

    // [Option(int)] describes that parameter is passed by index
    [Command("escape")]
    public void UrlEscape([Option(0)]string input)
    {
        Console.WriteLine(Uri.EscapeDataString(input));
    }

    // define async method returns Task
    [Command("timer")]
    public async Task Timer([Option(0)]uint waitSeconds)
    {
        Console.WriteLine(waitSeconds + " seconds");
        while (waitSeconds != 0)
        {
            // ConsoleAppFramework does not stop immediately on terminate command(Ctrl+C)
            // so you have to pass Context.CancellationToken to async method.
            await Task.Delay(TimeSpan.FromSeconds(1), Context.CancellationToken);
            waitSeconds--;
            Console.WriteLine(waitSeconds + " seconds");
        }
    }
}
```

You can call like

```
SampleApp.exe -n "foo" -r 3
SampleApp.exe escape http://foo.bar/
SampleApp.exe timer 10
```

Automatically Class/Method command routing
---
ConsoleAppFramework can create easily to many command application. You can write many class, methods and select by `class method` command like MVC application. It is useful to manage application specified batch programs. Uploading single binary and execute it, or git pull and run by `dotnet run -- [command] [option]` on CI.

```csharp
using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

// Entrypoint.
class Program
{
    static async Task Main(string[] args)
    {
        // don't pass <T>.
        await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync(args);
    }
}

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

You can call `{TypeName} {MethodName}` like

```
SampleApp.exe foo echo -msg "aaaaa"
SampleApp.exe foo sum -x 100 -y 200
SampleApp.exe bar hello2
```

`help` describe the method list

```
> SampleApp.exe help
Usage: SampleApp <Command>

Commands:
  foo echo
  foo sum
  bar hello2
```

`[command] help` shows command details.

```
> SampleApp.exe Foo.Echo -help
Usage: SampleApp Foo.Echo [options...]

Options:
  -msg <String>     (Required)
```

> Commands are searched from loaded assemblies, when does not touch other assemblies type, it will be trimmed and can not load it. In that case, use `RunConsoleAppFrameworkAsync(searchAssemblies: )` option to pass target assembly, for example `searchAssemblies: new [] { typeof(Foo).Assembly }`.

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
> SampleApp.exe -array [10,20,30] -person {"Age":10,"Name":"foo"}
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
    
    [Command("exitwithtask")]
    public async Task<int> ExitCodeWithTask()
    {
        return 54321;
    }
}
```

> **NOTE**: If the method throws an unhandled exception, ConsoleAppFramework always set `1` to the exit code.

CommandAttribute
---
`CommandAttribute` enables subscommand on `RunConsoleAppFramework<T>()`(for single type CLI app), changes command name on `RunConsoleAppFramework()`(for muilti type command routing), also describes the description.

```
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

```
RunConsoleAPpFramework();

public class App2 : ConsoleAppBase
{
    // routing command: `app2 exec`
    [Command("exec", "exec app.")]
    public void Exec1()
    {
    }
}


public class App3 : ConsoleAppBase
{
     // routing command: `app3 e2`
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

Daemon
---
`ConsoleAppBase.Context.CancellationToken` is lifecycle token of application. In default, ConsoleAppFramework does not abort on received terminate request, you can check `CancellationToken.IsCancellationRequested` and shutdown gracefully. If use infinite-loop, it becomes daemon program.

```csharp
public class Daemon : ConsoleAppBase
{
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
class Program
{
    static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder()
            .RunConsoleAppFrameworkAsync(args, new ConsoleAppOptions()
            {
                GlobalFilters = new ConsoleAppFilter[]{ 
                    new MutextFilter() { Order = -9999 } ,
                    new LogRunningTimeFilter() { Oder = -9998 }, 
            });
    }
}

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
In default, `Context.Logger` has `ILogger<ConsoleAppEngine>` and `ILogger<T>` can inject to constructor. Default `ConsoleLogger` format in `Host.CreateDefaultBuilder` is supernumerary and not suitable for console application. ConsoleAppFramework provides `SimpleConsoleLogger` to replace default ConsoleLogger.

```csharp
static async Task Main(string[] args)
{
    await Host.CreateDefaultBuilder()
        .ConfigureLogging(logging =>
        {
            // Replacing default console logger to SimpleConsoleLogger.
            logging.ReplaceToSimpleConsole();

            // Add ConsoleAppFramework.Logging.SimpleConsoleLogger.
            // logging.AddSimpleConsole();

            // Configure MinimumLogLevel(CreaterDefaultBuilder's default is Warning).
            logging.SetMinimumLevel(LogLevel.Trace);
        })
        .RunConsoleAppFrameworkAsync<Program>(args);
}
```

If you want to use high performance logger/output to file, also use [Cysharp/ZLogger](https://github.com/Cysharp/ZLogger/) that easy to integrate ConsoleAppFramework.

```csharp
await Host.CreateDefaultBuilder()
    .ConfigureLogging(x =>
    {
        x.ClearProviders();
        x.SetMinimumLevel(LogLevel.Trace);
        x.AddZLoggerConsole();
        x.AddZLoggerFile("fileName.log");
    })
    .RunConsoleAppFrameworkAsync(args);
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
class Program
{
    static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                // mapping config json to IOption<MyConfig>
                // requires "Microsoft.Extensions.Options.ConfigurationExtensions" package
                services.Configure<MyConfig>(hostContext.Configuration);
            })
            .RunConsoleAppFrameworkAsync<ConfigAppSample>(args);
    }
}

public class ConfigAppSample : ConsoleAppBase
{
    IOptions<MyConfig> config;

    // get configuration from DI.
    public ConfigAppSample(IOptions<MyConfig> config)
    {
        this.config = config;
    }

    public void ShowOption()
    {
        Console.WriteLine(config.Value.Bar);
        Console.WriteLine(config.Value.Foo);
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

DI also inject to filter.

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
}
```

ConsoleAppOptions
---
You can configure framework behaviour by ConsoleAppOptions.

```csharp
static async Task Main(string[] args)
{
    await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<Program>(args, new ConsoleAppOptions
    {
        StrictOption = true, // default is false.
        ShowDefaultCommand = false, // default is true
    });
}
```

```csharp
public class ConsoleAppOptions
{
    /// <summary>Argument parser uses strict(-short, --long) option. Default is false.</summary>
    public bool StrictOption { get; set; } = false;

    /// <summary>Show default command(help/version) to help. Default is true.</summary>
    public bool ShowDefaultCommand { get; set; } = true;

    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    public ConsoleAppFilter[]? GlobalFilters { get; set; }
}
```

In defauolt, The ConsoleAppFramework does not distinguish between the number of `-`.  For example, this method

```
public void Hello([Option("m", "Message to display.")]string message)
```

can pass argument by `-m`, `--message` and `-message`. This is styled like a go lang command. But if you want to strictly distinguish argument of `-`, set `StrcitOption = true`.

```
> SampleApp.exe help

Usage: SampleApp [options...]

Options:
  -m, --message <String>    Message to display. (Required)
```

Also, by default, the `help` and `version` commands appear as help, which can be hidden by setting `ShowDefaultCommand = false`.

Web Interface with Swagger
---
ConsoleAppFramework.WebHosting is support to expose web interface and swagger(with executable api document). It is useful for debugging.

NuGet: [ConsoleAppFramework.WebHosting](https://www.nuget.org/packages/ConsoleAppFramework.WebHosting)

```
Install-Package ConsoleAppFramework.WebHosting
```

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
            .RunConsoleAppFrameworkWebHostingAsync("http://localhost:12345");
    }
}
```

in browser `http://localhost:12345`, launch swagger ui.

![image](https://user-images.githubusercontent.com/46207/55614839-e8a95d00-57c8-11e9-89d5-ab0e7830e401.png)

Publish to executable file
---
[dotnet publish](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish) to create executable file. [.NET Core 3.0 offers Single Executable File](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-core-3-0) via `PublishSingleFile`.

Here is the sample `.github/workflows/build-release.yml` of GitHub Actions.

```yml
name: Build-Release

on:
  push:
    branches:
      - "**"

jobs:
  build-dotnet:
    runs-on: ubuntu-latest
    env:
      DOTNET_CLI_TELEMETRY_OPTOUT: 1
      DOTNET_SKIP_FIRST_TIME_EXPERIENCE: 1
      NUGET_XMLDOC_MODE: skip
    steps:
      - uses: actions/checkout@v2
      - uses: actions/setup-dotnet@v1
        with:
          dotnet-version: 3.1.101

      # Build
      - run: dotnet publish -c Release --self-contained /p:PublishSingleFile=true /p:IncludeSymbolsInSingleFile=true -r win-x64 -o ./bin/win-x64
      - run: dotnet publish -c Release --self-contained /p:PublishSingleFile=true /p:IncludeSymbolsInSingleFile=true -r linux-x64 -o ./bin/linux-x64
      - run: dotnet publish -c Release --self-contained /p:PublishSingleFile=true /p:IncludeSymbolsInSingleFile=true -r osx-x64 -o ./bin/osx-x64

      # Store artifacts.
      - uses: actions/upload-artifact@v2
        with:
          name: SampleApp
          path: ./src/SampleApp/bin/Release/
```

CLI tool can use [.NET Core Local/Global Tools](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools). If you want to create it, check the [Global Tools how to create](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-create) or [Local Tools introduction](https://andrewlock.net/new-in-net-core-3-local-tools/).

License
---
This library is under the MIT License.
