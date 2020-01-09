ConsoleAppFramework
===
[![CircleCI](https://circleci.com/gh/Cysharp/ConsoleAppFramework.svg?style=svg)](https://circleci.com/gh/Cysharp/ConsoleAppFramework)

ConsoleAppFramework is an infrastructure of creating CLI(Command-line interface) tools, daemon, and multi batch application.

// image

ConsoleAppFramework is built on [.NET Generic Host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host), you can use configuration, logging, DI, lifetime management by Microsoft.Extensions packages. ConsoleAppFramework do parameter binding from string args, routing multi command, dotnet style help builder, etc.

This concept is same as [Laravel Zero](https://laravel-zero.com/) of PHP. Similar competitor is [dotnet/command-line-api](https://github.com/dotnet/command-line-api)'s `System.CommandLine.Hosting` + `System.CommandLine.DragonFruit` but it is preview and currently not productivity.

NuGet: [ConsoleAppFramework](https://www.nuget.org/packages/ConsoleAppFramework)

```
Install-Package ConsoleAppFramework
```

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

`help` command(or no argument to pass) shows there detail. This help format is same as `dotnet` command.

```
> SampleApp.exe help
Usage: SampleApp [options...]

Options:
  -n, -name <String>     name of send user. (Required)
  -r, -repeat <Int32>    repeat count. (Default: 3)
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

Multi Batch Application
---
ConsoleAppFramework allows the multi batch application. You can write many class, methods and select by first-argument. It is useful to manage application specified batch programs. Uploading single binary and execute it, or git pull and run by `dotnet run [command] [option]` on CI.

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

You can call `{TypeName}.{MethodName}` like

```
SampleApp.exe Foo.Echo -msg "aaaaa"
SampleApp.exe Foo.Sum -x 100 -y 200
SampleApp.exe Bar.Hello2
```

`help` describe the method list

```
> SampleApp.exe help
Usage: SampleApp <Command>

Commands:
  Foo.Echo
  Foo.Sum
  Bar.Hello2
```

`[command] -help` shows command details.

> SampleApp.exe Foo.Echo -help
Usage: SampleApp Foo.Echo [options...]

Options:
  -msg <String>     (Required)
```

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

> JSON serializer is `System.Text.Json`. You can pass `JsonSerializerOptions` to SerivceProvider when you want to configure serializer behavior.

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
If the method returns `int` or `Task<int>` value, ConsoleAppFramework will set the return value to the exit code.

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
            while (!Context.CancellationToken.IsCancellationRequested)
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
                await Task.Delay(TimeSpan.FromMinutes(1), Context.CancellationToken);
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

Interceptor
---
Interceptor can hook before/after batch running event. You can imprement `IConsoleAppInterceptor` for it.

`ConsoleAppContext.Timestamp` has start time so if subtraction from now, get elapsed time.

```csharp
public class LogRunningTimeInterceptor : IConsoleAppInterceptor
{
    public ValueTask OnEngineBeginAsync(IServiceProvider serviceProvider, ILogger<ConsoleAppEngine> logger)
    {
        return default;
    }

    public ValueTask OnEngineCompleteAsync(ConsoleAppContext context, string? errorMessageIfFailed, Exception? exceptionIfExists)
    {
        return default;
    }

    public ValueTask OnMethodBeginAsync(ConsoleAppContext context)
    {
        context.Logger.LogInformation("Call method at " + context.Timestamp.ToLocalTime()); // LocalTime for human readable time
        return default;
    }

    public ValueTask OnMethodEndAsync()
    {
        context.Logger.LogInformation("Call method Completed, Elapsed:" + (DateTimeOffset.UtcNow - context.Timestamp));
        return default;
    }
}
```

In default, ConsoleAppFramework does not prevent double startup but if create interceptor, can do. 

```csharp
public class MutexInterceptor : IConsoleAppInterceptor
{
    Mutex mutex;
    bool hasHandle = false;

    public ValueTask OnEngineBeginAsync(IServiceProvider serviceProvider, ILogger<ConsoleAppEngine> logger)
    {
        mutex = new Mutex(false, Assembly.GetEntryAssembly().GetName().Name);
        if (!mutex.WaitOne(0, false))
        {
            hasHandle = true;
            throw new Exception("already running another process.");
        }

        return default;
    }

    public ValueTask OnEngineCompleteAsync(ConsoleAppContext context, string? errorMessageIfFailed, Exception? exceptionIfExists)
    {
        if (hasHandle)
        {
            mutex.ReleaseMutex();
        }
        mutex.Dispose();
        return default;
    }

    public ValueTask OnMethodBeginAsync(ConsoleAppContext context)
    {
        return default;
    }

    public ValueTask OnMethodEndAsync()
    {
        return default;
    }
}
```

There interceptor can pass to startup.

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder()
            .RunConsoleAppFrameworkAsync(args, new LogRunningTimeInterceptor());
    }
}
```

If you want to use multiple interceptor, you can use `CompositeConsoleAppInterceptor`.

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder()
            .RunConsoleAppFrameworkAsync(args, new CompositeConsoleAppInterceptor
            {
                new LogRunningTimeInterceptor(),
                new MutexInterceptor()
            });
    }
}
```

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
    public Program(IOptions<MyConfig> config)
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

for the details, please see [.NET Core Generic Host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host) documantation.

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

ConsoleAppContext
---
ConsoleAppContext is injected to property on method executing. It has four properties.

```csharp
public string[] Arguments { get; }
public DateTime Timestamp { get; }
public CancellationToken CancellationToken { get; }
public ILogger<ConsoleAppEngine> Logger { get; }
```

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
[dotnet publish](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish) to create executable file. [.NET Core 3.0 offers Single Executable File](https://docs.microsoft.com/ja-jp/dotnet/core/whats-new/dotnet-core-3-0) via `PublishSingleFile`.

Here is the sample `.config.yml` of [CircleCI](http://circleci.com).

```yml
version: 2.1
executors:
  dotnet:
    docker:
      - image: mcr.microsoft.com/dotnet/core/sdk:3.0
    environment:
      DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
      NUGET_XMLDOC_MODE: skip
jobs:
  publish-all:
    executor: dotnet
    steps:
      - checkout
      - run: dotnet publish -c Release --self-contained /p:PublishSingleFile=true /p:IncludeSymbolsInSingleFile=true -r win-x64 -o ./bin/win-x64
      - run: dotnet publish -c Release --self-contained /p:PublishSingleFile=true /p:IncludeSymbolsInSingleFile=true -r linux-x64 -o ./bin/linux-x64
      - run: dotnet publish -c Release --self-contained /p:PublishSingleFile=true /p:IncludeSymbolsInSingleFile=true -r osx-x64 -o ./bin/osx-x64
      - store_artifacts:
          path: ./bin/
          destination: ./bin/
workflows:
  version: 2
  publish:
    jobs:
      - publish-all
```

CLI tool can use [.NET Core Local/Global Tools](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools). If you want to create it, check the [Global Tools how to create](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-create) or [Local Tools introduction](https://andrewlock.net/new-in-net-core-3-local-tools/).

License
---
This library is under the MIT License.
