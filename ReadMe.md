MicroBatchFramework
===
[![CircleCI](https://circleci.com/gh/Cysharp/MicroBatchFramework.svg?style=svg)](https://circleci.com/gh/Cysharp/MicroBatchFramework)

MicroBatchFramework is an infrastructure of creating CLI(Command-line interface) tools, daemon, and multiple contained batch program. Easy to bind argument to the simple method definition. It built on [.NET Generic Host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host) so you can configure Configuration, Logging, DI, etc can load by the standard way.

NuGet: [MicroBatchFramework](https://www.nuget.org/packages/MicroBatchFramework)

```
Install-Package MicroBatchFramework
```

Single Contained Batch
---

Batch can write by simple method, argument is automatically binded to parameter.

```csharp
using MicroBatchFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

// Entrypoint, create from the .NET Core Console App.
class Program
{
    // C# 7.1(update lang version)
    static async Task Main(string[] args)
    {
        // you can use new HostBuilder() instead of CreateDefaultBuilder
        await BatchHost.CreateDefaultBuilder().RunBatchEngineAsync<MyFirstBatch>(args);
    }
}

// Batch definition.
public class MyFirstBatch : BatchBase // inherit BatchBase
{
    // allows void/Task return type, parameter allows all types(deserialized by Utf8Json and can pass by JSON string)
    public void Hello(string name, int repeat = 3)
    { 
        for (int i = 0; i < repeat; i++)
        {
            this.Context.Logger.LogInformation($"Hello My Batch from {name}");
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

`help` command shows there detail.

```
> SampleApp.exe help
-n, -name: name of send user.
-r, -repeat: [default=3]repeat count.
```

You can use `CommandAttribute` to create multi command program.

```csharp
public class MyFirstBatch : BatchBase
{
    public void Hello(
        [Option("n", "name of send user.")]string name,
        [Option("r", "repeat count.")]int repeat = 3)
    {
        for (int i = 0; i < repeat; i++)
        {
            this.Context.Logger.LogInformation($"Hello My Batch from {name}");
        }
    }

    [Command("version")]
    public void ShowVersion()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyFileVersionAttribute>()
            .Version;
        Console.WriteLine(version);
    }

    // [Option(int)] describes that parameter is passed by index
    [Command("escape")]
    public void UrlEscape([Option(0)]string input)
    {
        Console.WriteLine(Uri.EscapeDataString(input));
    }

    [Command("timer")]
    public async Task Timer([Option(0)]uint waitSeconds)
    {
        Console.WriteLine(waitSeconds + " seconds");
        while (waitSeconds != 0)
        {
            // MicroBatchFramework does not stop immediately on terminate command(Ctrl+C)
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
SampleApp.exe version
SampleApp.exe escape http://foo.bar/
SampleApp.exe timer 10
```

Multi Contained Batch
---
MicroBatchFramework allows the multi contained batch. You can write many class, methods and select by first-argument.

```csharp
using MicroBatchFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

// Entrypoint.
class Program
{
    static async Task Main(string[] args)
    {
        await BatchHost.CreateDefaultBuilder().RunBatchEngineAsync(args); // don't pass <T>.
    }
}

// Batches.
public class Foo : BatchBase
{
    public void Echo(string msg)
    {
        this.Context.Logger.LogInformation(msg);
    }

    public void Sum(int x, int y)
    {
        this.Context.Logger.LogInformation((x + y).ToString());
    }
}

public class Bar : BatchBase
{
    public void Hello2()
    {
        this.Context.Logger.LogInformation("H E L L O");
    }
}
```

You can call like

```
SampleApp.exe Foo.Echo -msg "aaaaa"
SampleApp.exe Foo.Sum -x 100 -y 200
SampleApp.exe Bar.Hello2
```

`list` command shows all invokable methods.

```
> SampleApp.exe list
Foo.Echo
Foo.Sum
Bar.Hello2
```

also use with `help`

```
> SampleApp.exe help Foo.Echo
-msg: String
```

Daemon
---

`BatchBase(this).Context.CancellationToken` is lifecycle token of batch. In default, MicroBatchFramework does not abort on received terminate request, you can check `CancellationToken.IsCancellationRequested` and shutdown gracefully. If use infinite-loop, it becomes daemon program.

```csharp
public class Daemon : BatchBase
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
                    Context.Logger.LogDebug("Wait One Minutes");
                }
                catch (Exception ex)
                {
                    // error occured but continue to run(or terminate).
                    Context.Logger.LogError(ex, "Found error");
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
Interceptor can hook before/after batch running event. You can imprement `IBatchInterceptor` for it.

`BatchContext.Timestamp` has start time so if subtraction from now, get elapsed time.

```csharp
public class LogRunningTimeInterceptor : IBatchInterceptor
{
    public ValueTask OnBatchEngineBeginAsync(IServiceProvider serviceProvider, ILogger<BatchEngine> logger)
    {
        return default;
    }

    public ValueTask OnBatchEngineEndAsync()
    {
        return default;
    }

    public ValueTask OnBatchRunBeginAsync(BatchContext context)
    {
        context.Logger.LogInformation("Batch Begin at " + context.Timestamp.ToLocalTime()); // LocalTime for human readable time
        return default;
    }

    public ValueTask OnBatchRunCompleteAsync(BatchContext context, string errorMessageIfFailed, Exception exceptionIfExists)
    {
        context.Logger.LogInformation("Batch Completed, Elapsed:" + (DateTimeOffset.UtcNow - context.Timestamp));
        return default;
    }
}
```

In default, MicroBatchFramework does not prevent double startup but if create interceptor, can do. 

```csharp
public class MutexInterceptor : IBatchInterceptor
{
    Mutex mutex;
    bool hasHandle = false;

    public ValueTask OnBatchEngineBeginAsync(IServiceProvider serviceProvider, ILogger<BatchEngine> logger)
    {
        mutex = new Mutex(false, Assembly.GetEntryAssembly().GetName().Name);
        if (!mutex.WaitOne(0, false))
        {
            hasHandle = true;
            throw new Exception("already running another process.");
        }

        return default;
    }

    public ValueTask OnBatchEngineEndAsync()
    {
        if (hasHandle)
        {
            mutex.ReleaseMutex();
        }
        mutex.Dispose();
        return default;
    }

    public ValueTask OnBatchRunBeginAsync(BatchContext context)
    {
        return default;
    }

    public ValueTask OnBatchRunCompleteAsync(BatchContext context, string errorMessageIfFailed, Exception exceptionIfExists)
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
        await BatchHost.CreateDefaultBuilder()
            .RunBatchEngineAsync(args, new LogRunningTimeInterceptor());
    }
}
```

If you want to use multiple interceptor, you can use `CompositeBatchInterceptor`.

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        await BatchHost.CreateDefaultBuilder()
            .RunBatchEngineAsync(args, new CompositeBatchInterceptor
            {
                new LogRunningTimeInterceptor(),
                new MutexInterceptor()
            });
    }
}
```

Configure Configuration
---
MicroBatchFramework is just a infrastructure. You can add appsettings.json or other configs as .NET Core offers via `ConfigureAppConfiguration`.
You can add `appsettings.json` and `appsettings.<env>.json` and typesafe load via map config to Class w/IOption.

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
        await BatchHost.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                // mapping config json to IOption<MyConfig>
                services.Configure<MyConfig>(hostContext.Configuration);
            })
            .RunBatchEngineAsync<MyFirstBatch>(args);
    }
}

public class MyFirstBatch : BatchBase
{
    IOptions<MyConfig> config;

    // get configuration from DI.
    public MyFirstBatch(IOptions<MyConfig> config)
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

`BatchHost.CreateDefaultBuilder()` is similar as `WebHost.CreateDefaultBuilder` on ASP.NET Core, that setup like below.

```csharp
var builder = new HostBuilder();

// set the content root to executing assembly's location.
builder.UseContentRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
builder.ConfigureAppConfiguration((hostingContext, config) =>
{
    var env = hostingContext.HostingEnvironment;

    // Get/Set Environement Name.
    env.ApplicationName = Assembly.GetExecutingAssembly().GetName().Name;
    if (string.IsNullOrWhiteSpace(contextEnvironmentVariable))
    {
        contextEnvironmentVariable = "NETCORE_ENVIRONMENT";
    }
    env.EnvironmentName = System.Environment.GetEnvironmentVariable(contextEnvironmentVariable) ?? "Production";

    // Load settings from JSON file.
    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

    // If EnvironmentName is "Development", try to load UserSecrets.
    if (env.IsDevelopment())
    {
        var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
        if (appAssembly != null)
        {
            config.AddUserSecrets(appAssembly, optional: true);
        }
    }

    // Load settings from Environment variables.
    config.AddEnvironmentVariables();
});
builder.ConfigureLogging(logging =>
{
    // if embeded SimpleConsoleLogger(default is true), setup logging(MinLogLevel's default is Debug).
    if (useSimpleConosoleLogger)
    {
        builder.ConfigureLogging(logging =>
        {
            logging.AddSimpleConsole();
            logging.AddFilter<SimpleConsoleLoggerProvider>((category, level) =>
            {
                // omit system message
                if (category.StartsWith("Microsoft.Extensions.Hosting.Internal"))
                {
                    if (level <= LogLevel.Debug) return false;
                }

                return level >= minSimpleConsoleLoggerLogLevel;
            });
        });
    }
});

return builder;
```

DI
---
You can use DI(constructor injection) by GenericHost.

```csharp
IOptions<MyConfig> config;
ILogger<MyFirstBatch> logger;

public MyFirstBatch(IOptions<MyConfig> config, ILogger<MyFirstBatch> logger)
{
    this.config = config;
    this.logger = logger;
}
```

BatchContext
---
BatchContext is injected to property on method executing. It has four properties.

```csharp
public string[] Arguments { get; private set; }
public DateTime Timestamp { get; private set; }
public CancellationToken CancellationToken { get; private set; }
public ILogger<BatchEngine> Logger { get; private set; }
```

Web Interface with Swagger
---
MicroBatchFramework.WebHosting is support to expose web interface and swagger(with executable api document). It is useful for debugging.

NuGet: [MicroBatchFramework.WebHosting](https://www.nuget.org/packages/MicroBatchFramework.WebHosting)

```
Install-Package MicroBatchFramework.WebHosting
```

```csharp
public class Program
{
    public static async Task Main(string[] args)
    {
        await new WebHostBuilder().RunBatchEngineWebHosting("http://localhost:12345");
    }
}
```

in browser `http://localhost:12345`, launch swagger ui.

![image](https://user-images.githubusercontent.com/46207/55614839-e8a95d00-57c8-11e9-89d5-ab0e7830e401.png)

Publish to executable file
---
[dotnet publish](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish) to create executable file.

Here is the sample `.config.yml` of [Circle CI](http://circleci.com).

```yml
version: 2.1
executors:
  dotnet:
    docker:
      - image: mcr.microsoft.com/dotnet/core/sdk:2.2
    environment:
      DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
      NUGET_XMLDOC_MODE: skip
jobs:
  publish-all:
    executor: dotnet
    steps:
      - checkout
      - run: dotnet publish -c Release --self-contained -r win-x64 -o ./bin/win-x64
      - run: dotnet publish -c Release --self-contained -r win-x64 -o ./bin/linux-x64
      - run: dotnet publish -c Release --self-contained -r win-x64 -o ./bin/osx-x64
      - store_artifacts:
          path: ./bin/
          destination: ./bin/
workflows:
  version: 2
  publish:
    jobs:
      - publish-all
```

CLI tool can use [.NET Core Global Tools](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools). If you want to create it, check the [Global Tools how to create](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-create).

Pack to Docker and deploy
---
If you hosting the batch to server, recommend to use container. Add Dockerfile like below.

```dockerfile
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS sdk
WORKDIR /workspace
COPY . .
RUN dotnet publish ./MicroBatchFrameworkSample.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/runtime:2.2
COPY --from=sdk /app .
ENTRYPOINT ["dotnet", "MicroBatchFrameworkSample.dll"]
```

And docker build, send to any container registory. Here is the sample of deploy AWS ECR by Circle CI.

```yml
version: 2.1
orbs:
  aws-ecr: circleci/aws-ecr@3.1.0
workflows:
  build-push:
    jobs:
      # see: https://circleci.com/orbs/registry/orb/circleci/aws-ecr
      - aws-ecr/build_and_push_image:
          repo: "microbatchsample"
```

and set the `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_ECR_ACCOUNT_URL, AWS_REGION` environment variables on Circle CI.

for example, run by [AWS Batch](https://aws.amazon.com/jp/batch/), you can host easily and log can view on CloudWatch.

![image](https://user-images.githubusercontent.com/46207/55616375-a6821a80-57cc-11e9-9d3a-a0691e631f28.png)

If you want to create complex workflow, you can use any worlkflow engine like [luigi](https://github.com/spotify/luigi), [Apache Airflow](https://github.com/apache/airflow), etc.

Scheduling(cron, taskscheduler)
---
If you host on AWS Batch, you can use CloudWatch Events to simple event scheduling trigger. If hosting to kubernetes, you can use Kubernetes CronJob.

Author Info
---
This library is mainly developed by Yoshifumi Kawai(a.k.a. neuecc).  
He is the CEO/CTO of Cysharp which is a subsidiary of [Cygames](https://www.cygames.co.jp/en/).  
He is awarding Microsoft MVP for Developer Technologies(C#) since 2011.  
He is known as the creator of [UniRx](https://github.com/neuecc/UniRx/) and [MessagePack for C#](https://github.com/neuecc/MessagePack-CSharp/).

License
---
This library is under the MIT License.
