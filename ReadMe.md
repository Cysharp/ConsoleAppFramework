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
    static async Task Main(string[] args)
    {
        await new HostBuilder()
            .ConfigureLogging(x => x.AddConsole())
            .RunBatchEngine<MyFirstBatch>(args);
    }
}

// Batch definition.
public class MyFirstBatch : BatchBase // inherit BatchBase
{
    // allows void/Task return type, parameter allows all types(deserialized by Utf8Json and can pass by JSON string)
    public void Hello(string name, int no = 99)
    { 
        for (int i = 0; i < repeat; i++)
        {
            this.Context.Logger.LogInformation($"Hello My Batch from {name}");
        }
    }
}
```

You can execute command like `SampleApp.exe -name "foo" -no 3`.

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
        await new HostBuilder()
            .ConfigureLogging(x => x.AddConsole())
            .RunBatchEngine(args); // don't pass <T>.
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
WIP

Interceptor
---
WIP

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
