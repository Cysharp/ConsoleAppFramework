MicroBatchFramework
===
WIP but welcome to try and your impressions. NuGet: [MicroBatchFramework](https://www.nuget.org/packages/MicroBatchFramework)

```
Install-Package MicroBatchFramework -Pre
```

Single Contained Batch
---
MicroBatchFramework is built on [.NET Generic Host](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host) so you can configure Configuration, Logging, DI, etc can load by standard way.

Batch can write by simple method, argument is automaticaly binded to parameter.

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
        await new HostBuilder().RunBatchEngineAsync<MyFirstBatch>(args);
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
        await new HostBuilder().RunBatchEngineAsync(args); // don't pass <T>.
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

Single Contained Batch with Config loading
---
MicroBatchFramework is just a infrastructure. You can add appsettings.json or other configs as .NET Core offers.
You can add `appsettings.json` and `appsettings.<env>.json` and typesafe load via map config to Class w/IOption.

```csharp
using MicroBatchFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

// Entrypoint.
class Program
{
    static async Task Main(string[] args)
    {
        await new HostBuilder()
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                // Set Environment variable "NETCORE_ENVIRONMENT" as Production | Staging | Development
                hostContext.HostingEnvironment.EnvironmentName = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT") ?? "production";
                config.SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                    .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional:true, reloadOnChange:true);
            })
            .ConfigureServices((hostContext, services) => {
                services.AddOptions();
                // mapping json element to class
                services.Configure<AppConfig>(hostContext.Configuration.GetSection("AppConfig"));
            })
            .ConfigureLogging(x => {
                // using MicroBatchFramework.Logging;
                x.AddSimpleConsole();
            })
            .RunBatchEngineAsync<Baz>(args);
    }
}

// config mapping class
public class AppConfig
{
    public string MyValue { get; set; }
}

// Batche inject Config on constructor.
public class Baz : BatchBase
{
    private readonly IOptions<SingleContainedAppWithConfig.AppConfig> config;
    public Baz(IOptions<SingleContainedAppWithConfig.AppConfig> config)
    {
        this.config = config;
    }
    public void Hello3()
    {
        this.Context.Logger.LogInformation(config.Value.MyValue);
    }
}
```

You can add appsettings.json.

```json
{
  "AppConfig": {
    "GlobalValue": "GLOBAL VALUE!!!!",
    "EnvValue": "ENV VALUE!!!!"
  }
}
```

Also add appsettings.Production.json and appsettings.Development.json to override appsettings.json value.

```json
{
  "AppConfig": {
    "EnvValue": "ENV VALUE!!!!(PRODUCTION)"
  }
}
```

```json
{
  "AppConfig": {
    "EnvValue": "ENV VALUE!!!!(DEVELOPMENT)"
  }
}
```

You can call like `SampleApp.exe Baz.Hello3`.

When you don't set environment variable `NETCORE_ENVIRONMENT` or set it as `Production`, appsettings.json will override via appsettings.Production.json.
Output will be.

```
GlobalValue: GLOBAL VALUE!!!!, EnvValue: ENV VALUE!!!!(PRODUCTION)
```

If you set `NETCORE_ENVIRONMENT` as `Development`, EnvValue will be override to Dev.

```
GlobalValue: GLOBAL VALUE!!!!, EnvValue: ENV VALUE!!!!(DEVELOPMENT)
```

Daemon
---
WIP

Interceptor
---
WIP

Configure Configuration
---

DI
---

Web Interface with Swagger
---
WIP

Pack to Docker
---
WIP

Scheduling(cron, taskscheduler)
---
WIP
