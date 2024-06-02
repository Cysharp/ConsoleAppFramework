ConsoleAppFramework
===
[![GitHub Actions](https://github.com/Cysharp/ConsoleAppFramework/workflows/Build-Debug/badge.svg)](https://github.com/Cysharp/ConsoleAppFramework/actions) [![Releases](https://img.shields.io/github/release/Cysharp/ConsoleAppFramework.svg)](https://github.com/Cysharp/ConsoleAppFramework/releases)

ConsoleAppFramework v5 is Zero Dependency, Zero Overhead, Zero Reflection, Zero Allocation, AOT Safe CLI Framework powered by C# Source Generator; achieves exceptionally high performance and minimal binary size. Leveraging the latest features of .NET 8 and C# 12 ([IncrementalGenerator](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md), [managed function pointer](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/function-pointers#function-pointers-1), [params arrays and default values lambda expression](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions#input-parameters-of-a-lambda-expression), [`ISpanParsable<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.ispanparsable-1), [`PosixSignalRegistration`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.posixsignalregistration), etc.), this library ensures maximum performance while maintaining flexibility and extensibility.

![image](https://github.com/Cysharp/ConsoleAppFramework/assets/46207/db4bf599-9fe0-4ce4-801f-0003f44d5628)
> Set `RunStrategy=ColdStart WarmupCount=0` to calculate the cold start benchmark, which is suitable for CLI application.

The magical performance is achieved by statically generating everything and parsing inline. Let's take a look at a minimal example:

```csharp
using ConsoleAppFramework;

// args: ./cmd --foo 10 --bar 20
ConsoleApp.Run(args, (int foo, int bar) => Console.WriteLine($"Sum: {foo + bar}"));
```

Unlike typical Source Generators that use attributes as keys for generation, ConsoleAppFramework analyzes the provided lambda expressions or method references and generates the actual code body of the Run method.

```csharp
namespace ConsoleAppFramework;

internal static partial class ConsoleApp
{
    public static void Run(string[] args, Action<int, int> command)
    {
        if (TryShowHelpOrVersion(args, 2, -1)) return;

        var arg0 = default(int);
        var arg0Parsed = false;
        var arg1 = default(int);
        var arg1Parsed = false;

        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                var name = args[i];

                switch (name)
                {
                    case "--foo":
                    {
                        if (!int.TryParse(args[++i], out arg0)) { ThrowArgumentParseFailed("foo", args[i]); }
                        arg0Parsed = true;
                        break;
                    }
                    case "--bar":
                    {
                        if (!int.TryParse(args[++i], out arg1)) { ThrowArgumentParseFailed("bar", args[i]); }
                        arg1Parsed = true;
                        break;
                    }
                    default:
                        if (string.Equals(name, "--foo", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!int.TryParse(args[++i], out arg0)) { ThrowArgumentParseFailed("foo", args[i]); }
                            arg0Parsed = true;
                            break;
                        }
                        if (string.Equals(name, "--bar", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!int.TryParse(args[++i], out arg1)) { ThrowArgumentParseFailed("bar", args[i]); }
                            arg1Parsed = true;
                            break;
                        }
                        ThrowArgumentNameNotFound(name);
                        break;
                }
            }
            if (!arg0Parsed) ThrowRequiredArgumentNotParsed("foo");
            if (!arg1Parsed) ThrowRequiredArgumentNotParsed("bar");

            command(arg0!, arg1!);
        }
        catch (Exception ex)
        {
            Environment.ExitCode = 1;
            if (ex is ValidationException)
            {
                LogError(ex.Message);
            }
            else
            {
                LogError(ex.ToString());
            }
        }
    }

    static partial void ShowHelp(int helpId)
    {
        Log("""
Usage: [options...] [-h|--help] [--version]

Options:
  --foo <int>     (Required)
  --bar <int>     (Required)
""");
    }
}
```

As you can see, the code is straightforward and simple, making it easy to imagine the execution cost of the framework portion. That's right, it's zero. This technique was influenced by Rust's macros. Rust has [Attribute-like macros and Function-like macros](https://doc.rust-lang.org/book/ch19-06-macros.html), and ConsoleAppFramework's generation can be considered as Function-like macros.

The `ConsoleApp` class, along with everything else, is generated entirely by the Source Generator, resulting in no dependencies, including ConsoleAppFramework itself. This characteristic should contribute to the small assembly size and ease of handling, including support for Native AOT.

Moreover, CLI applications typically involve single-shot execution from a cold start. As a result, common optimization techniques such as dynamic code generation (IL Emit, ExpressionTree.Compile) and caching (ArrayPool) do not work effectively. ConsoleAppFramework generates everything statically in advance, achieving performance equivalent to optimized hand-written code without reflection or boxing.

ConsoleAppFramework offers a rich set of features as a framework. The Source Generator analyzes which modules are being used and generates the minimal code necessary to implement the desired functionality.

* SIGINT/SIGTERM(Ctrl+C) handling with gracefully shutdown via `CancellationToken`
* Filter(middleware) pipeline to intercept before/after execution
* Exit code management
* Support for async commands
* Registration of multiple commands
* Registration of nested commands
* Setting option aliases and descriptions from code document comment
* `System.ComponentModel.DataAnnotations` attribute-based Validation
* Dependency Injection for command registration by type and public methods
* `Microsoft.Extensions`(Logging, Configuration, etc...) integration
* High performance value parsing via `ISpanParsable<T>`
* Parsing of params arrays
* Parsing of JSON arguments
* Help(`-h|--help`) option builder
* Default show version(`--version`) option

As you can see from the generated output, the help display is also fast. In typical frameworks, the help string is constructed after the help invocation. However, in ConsoleAppFramework, the help is embedded as string constants, achieving the absolute maximum performance that cannot be surpassed!

Getting Started
--
This library is distributed via NuGet, minimal requirement is .NET 8 and C# 12.

> PM> Install-Package [ConsoleAppFramework](https://www.nuget.org/packages/ConsoleAppFramework)

ConsoleAppFramework is an analyzer (Source Generator) and does not have any dll references. When referenced, the entry point class `ConsoleAppFramework.ConsoleApp` is generated internally.

The first argument of `Run` or `RunAsync` can be `string[] args`, and the second argument can be any lambda expression, method, or function reference. Based on the content of the second argument, the corresponding function is automatically generated.

```csharp
using ConsoleAppFramework;

ConsoleApp.Run(args, (string name) => Console.WriteLine($"Hello {name}"));
```

You can execute command like `sampletool --name "foo"`.

* The return value can be `void`, `int`, `Task`, or `Task<int>`
    * If an `int` is returned, that value will be set to `Environment.ExitCode`
* By default, option argument names are converted to `--lower-kebab-case`
    * For example, `jsonValue` becomes `--json-value`
    * Option argument names are case-insensitive, but lower-case matches faster

When passing a method, you can write it as follows:

```csharp
ConsoleApp.Run(args, Sum);

void Sum(int x, int y) => Console.Write(x + y);
```

Additionally, for static functions, you can pass them as function pointers. In that case, the managed function pointer arguments will be generated, resulting in maximum performance.

```csharp
unsafe
{
    ConsoleApp.Run(args, &Sum);
}

static void Sum(int x, int y) => Console.Write(x + y);
```

```csharp
public static unsafe void Run(string[] args, delegate* managed<int, int, void> command)
```

Unfortunately, currently [static lambdas cannot be assigned to function pointers](https://github.com/dotnet/csharplang/discussions/6746), so defining a named function is necessary.

When defining an asynchronous method using a lambda expression, the `async` keyword is required.

```csharp
// --foo, --bar
await ConsoleApp.RunAsync(args, async (int foo, int bar, CancellationToken cancellationToken) =>
{
    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    Console.WriteLine($"Sum: {foo + bar}");
});
```

You can use either the `Run` or `RunAsync` method for invocation. It is optional to use `CancellationToken` as an argument. This becomes a special parameter and is excluded from the command options. Internally, it uses [`PosixSignalRegistration`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.posixsignalregistration) to handle `SIGINT`, `SIGTERM`, and `SIGKILL`. When these signals are invoked (e.g., Ctrl+C), the CancellationToken is set to CancellationRequested. If `CancellationToken` is not used as an argument, these signals will not be handled, and the program will terminate immediately. For more details, refer to the [CancellationToken and Gracefully Shutdown](#cancellationtokengracefully-shutdown-and-timeout) section.

Option aliases and Help, Version
---
By default, if `-h` or `--help` is provided, or if no arguments are passed, the help display will be invoked.

```csharp
ConsoleApp.Run(args, (string message) => Console.Write($"Hello, {message}"));
```

```txt
Usage: [options...] [-h|--help] [--version]

Options:
  --message <string>     (Required)
```

In ConsoleAppFramework, instead of using attributes, you can provide descriptions and aliases for functions by writing Document Comments. This avoids the common issue in frameworks where arguments become cluttered with attributes, making the code difficult to read. With this approach, a natural writing style is achieved.

```csharp
ConsoleApp.Run(args, Commands.Hello);

static class Commands
{
    /// <summary>
    /// Display Hello.
    /// </summary>
    /// <param name="message">-m, Message to show.</param>
    public static void Hello(string message) => Console.Write($"Hello, {message}");
}
```

```txt
Usage: [options...] [-h|--help] [--version]

Display Hello.

Options:
  -m|--message <string>    Message to show. (Required)
```

To add aliases to parameters, list the aliases separated by `|` before the comma in the comment. For example, if you write a comment like `-a|-b|--abcde, Description.`, then `-a`, `-b`, and `--abcde` will be treated as aliases, and `Description.` will be the description.

Unfortunately, due to current C# specifications, lambda expressions and [local functions do not support document comments](https://github.com/dotnet/csharplang/issues/2110), so a class is required.

In addition to `-h|--help`, there is another special built-in option: `--version`. This displays the `AssemblyInformationalVersion` or `AssemblyVersion`.

Command
---
If you want to register multiple commands or perform complex operations (such as adding filters), instead of using `Run/RunAsync`, obtain the `ConsoleAppBuilder` using `ConsoleApp.Create()`. Call `Add`, `Add<T>`, or `UseFilter<T>` multiple times on the `ConsoleAppBuilder` to register commands and filters, and finally execute the application using `Run` or `RunAsync`.

```csharp
var app = ConsoleApp.Create();

app.Add("", (string msg) => Console.WriteLine(msg));
app.Add("echo", (string msg) => Console.WriteLine(msg));
app.Add("sum", (int x, int y) => Console.WriteLine(x + y));

// --msg
// echo --msg
// sum --x --y
app.Run(args);
```

The first argument of `Add` is the command name. If you specify an empty string `""`, it becomes the root command. Unlike parameters, command names are case-sensitive and cannot have multiple names.

With `Add<T>`, you can add multiple commands at once using a class-based approach, where public methods are treated as commands. If you want to write document comments for multiple commands, this approach allows for cleaner code, so it is recommended. Additionally, as mentioned later, you can also write clean code for Dependency Injection (DI) using constructor injection.

```csharp
var app = ConsoleApp.Create();
app.Add<MyCommands>();
app.Run(args);

public class MyCommands
{
    /// <summary>Root command test.</summary>
    /// <param name="msg">-m, Message to show.</param>
    [Command("")]
    public void Root(string msg) => Console.WriteLine(msg);

    /// <summary>Display message.</summary>
    /// <param name="msg">Message to show.</param>
    public void Echo(string msg) => Console.WriteLine(msg);

    /// <summary>Sum parameters.</summary>
    /// <param name="x">left value.</param>
    /// <param name="y">right value.</param>
    public void Sum(int x, int y) => Console.WriteLine(x + y);
}
```

When you check the registered commands with `--help`, it will look like this. Note that you can register multiple `Add<T>` and also add commands using `Add`.

```txt
Usage: [command] [options...] [-h|--help] [--version]

Root command test.

Options:
  -m|--msg <string>    Message to show. (Required)

Commands:
  echo    Display message.
  sum     Sum parameters.
```

By default, the command name is derived from the method name converted to `lower-kebab-case`. However, you can change the name to any desired value using the `[Command(string commandName)]` attribute.

If the class implements `IDisposable` or `IAsyncDisposable`, the Dispose or DisposeAsync method will be called after the command execution.

### Nested command

You can create a deep command hierarchy by adding commands with paths separated by space(` `) when registering them. This allows you to add commands at nested levels.

```csharp
var app = ConsoleApp.Create();

app.Add("foo", () => { });
app.Add("foo bar", () => { });
app.Add("foo bar barbaz", () => { });
app.Add("foo baz", () => { });

// Commands:
//   foo
//   foo bar
//   foo bar barbaz
//   foo baz
app.Run(args);
```

`Add<T>` can also add commands to a hierarchy by passing a `string commandPath` argument.

```csharp
var app = ConsoleApp.Create();
app.Add<MyCommands>("foo");

// Commands:
//  foo         Root command test.
//  foo echo    Display message.
//  foo sum     Sum parameters.
app.Run(args);
```

### Performance of Commands

TODO:NANIKA KAKU

Parse and Value Binding
---




// TODO:reason and policy of limitation of parsing

`[Argument]`

`bool`





             


`enum`
`nullable?`
`DateTime`

`ISpanParsable<T>`
#### default
#### json
#### params T[]


#### Custom Value Converter

// TODO:

```csharp
[AttributeUsage(AttributeTargets.Parameter)]
public class Vector3ParserAttribute : Attribute, IArgumentParser<Vector3>
{
    public static bool TryParse(ReadOnlySpan<char> s, out Vector3 result)
    {
        Span<Range> ranges = stackalloc Range[3];
        var splitCount = s.Split(ranges, ',');
        if (splitCount != 3)
        {
            result = default;
            return false;
        }

        float x;
        float y;
        float z;
        if (float.TryParse(s[ranges[0]], out x) && float.TryParse(s[ranges[1]], out y) && float.TryParse(s[ranges[2]], out z))
        {
            result = new Vector3(x, y, z);
            return true;
        }

        result = default;
        return false;
    }
}
```





CancellationToken(Gracefully Shutdown) and Timeout
---





Exit Code
---
If the method returns `int` or `Task<int>` or `ValueTask<int> value, ConsoleAppFramework will set the return value to the exit code.



> **NOTE**: If the method throws an unhandled exception, ConsoleAppFramework always set `1` to the exit code.




Attribute based parameters validation
---





Filter(Middleware) Pipline / ConsoleAppContext
---
Filters are provided as a mechanism to hook into the execution before and after. To use filters, define an `internal class` that implements `ConsoleAppFilter`.

```csharp
internal class NopFilter(ConsoleAppFilter next) : ConsoleAppFilter(next) // ctor needs `ConsoleAppFilter next` and call base(next)
{
    // implement InvokeAsync as filter body
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        try
        {
            /* on before */
            await Next.InvokeAsync(context, cancellationToken); // invoke next filter or command body
            /* on after */
        }
        catch
        {
            /* on error */
            throw;
        }
        finally
        {
            /* on finally */
        }
    }
}
```

Filters can be attached multiple times to "global", "class", or "method" using `UseFilter<T>` or `[ConsoleAppFilter<T>]`. The order of filters is global → class → method, and the execution order is determined by the definition order from top to bottom.

```csharp
var app = ConsoleApp.Create();

// global filters
app.UseFilter<NopFilter>(); //order 1
app.UseFilter<NopFilter>(); //order 2

app.Add<MyCommand>();
app.Run(args);

// per class filters
[ConsoleAppFilter<NopFilter>] // order 3
[ConsoleAppFilter<NopFilter>] // order 4
public class MyCommand
{
    // per method filters
    [ConsoleAppFilter<NopFilter>] // order 5
    [ConsoleAppFilter<NopFilter>] // order 6
    public void Echo(string msg) => Console.WriteLine(msg);
}
```

Filters allow various processes to be shared. For example, the process of measuring execution time can be written as follows:

```csharp
internal class LogRunningTimeFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        var startTime = Stopwatch.GetTimestamp();
        ConsoleApp.Log($"Execute command at {DateTime.UtcNow.ToLocalTime()}"); // LocalTime for human readable time
        try
        {
            await Next.InvokeAsync(context, cancellationToken);
            ConsoleApp.Log($"Command execute successfully at {DateTime.UtcNow.ToLocalTime()}, Elapsed: " + (Stopwatch.GetElapsedTime(startTime)));
        }
        catch
        {
            ConsoleApp.Log($"Command execute failed at {DateTime.UtcNow.ToLocalTime()}, Elapsed: " + (Stopwatch.GetElapsedTime(startTime)));
            throw;
        }
    }
}
```

In case of an exception, the `ExitCode` is usually `1`, and the stack trace is also displayed. However, by applying an exception handling filter, the behavior can be changed.

```csharp
internal class ChangeExitCodeFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        try
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) return;

            Environment.ExitCode = 9999; // change custom exit code
            ConsoleApp.LogError(ex.Message); // .ToString() shows stacktrace, .Message can avoid showing stacktrace to user.
        }
    }
}
```

Filters are executed after the command name routing is completed. If you want to prohibit multiple executions for each command name, you can use `ConsoleAppContext.CommandName` as the key.

```csharp
internal class PreventMultipleSameCommandInvokeFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        var basePath = Assembly.GetEntryAssembly()?.Location.Replace(Path.DirectorySeparatorChar, '_');
        var mutexKey = $"{basePath}$$${context.CommandName}"; // lock per command-name

        using var mutex = new Mutex(true, mutexKey, out var createdNew);
        if (!createdNew)
        {
            throw new Exception($"already running command:{context.CommandName} in another process.");
        }

        await Next.InvokeAsync(context, cancellationToken);
    }
}
```

If you want to pass values between filters or to commands, you can use `ConsoleAppContext.State`. For example, if you want to perform authentication processing and pass around the ID, you can write code like the following. Since `ConsoleAppContext` is an immutable record, you need to pass the rewritten context to Next using the `with` syntax.

```csharp
internal class AuthenticationFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid();
        var userId = await GetUserIdAsync();

        // setup new state to context
        var authedContext = context with { State = new ApplicationContext(requestId, userId) };
        await Next.InvokeAsync(authedContext, cancellationToken);
    }

    // get user-id from DB/auth saas/others
    async Task<int> GetUserIdAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        return 1999;
    }
}

record class ApplicationContext(Guid RequiestId, int UserId);
```

Commands can accept `ConsoleAppContext` as an argument. This allows using the values processed by filters.

```csharp
var app = ConsoleApp.Create();

app.UseFilter<AuthenticationFilter>();

app.Add("", (int x, int y, ConsoleAppContext context) =>
{
    var appContext = (ApplicationContext)context.State!;
    var requestId = appContext.RequiestId;
    var userId = appContext.UserId;

    Console.WriteLine($"Request:{requestId} User:{userId} Sum:{x + y}");
});

app.Run(args);
```

`ConsoleAppContext` also has a `ConsoleAppContext.Arguments` property that allows you to obtain the (`string[] args`) passed to Run/RunAsync.

### Sharing Filters Between Projects

`ConsoleAppFilter` is defined as `internal` for each project by the Source Generator, so the filters to be implemented must also be `internal`. Sharing at the csproj or DLL level is not possible, so source code needs to be shared by linking references.

```xml
<ItemGroup>
    <Compile Include="..\CommonProject\Filters.cs" />
</ItemGroup>
```

If you want to share via NuGet, you need to distribute the source code or distribute it in a format that includes the source code using `.props`.

### Performance of filter

In general frameworks, filters are dynamically added at runtime, resulting in a variable number of filters. Therefore, they need to be allocated using a dynamic array. In ConsoleAppFramework, the number of filters is statically determined at compile time, eliminating the need for any additional allocations such as arrays or lambda expression captures. The allocation amount is equal to the number of filter classes being used plus 1 (for wrapping the command method), resulting in the shortest execution path.

```csharp
app.UseFilter<NopFilter>();
app.UseFilter<NopFilter>();
app.UseFilter<NopFilter>();
app.UseFilter<NopFilter>();
app.UseFilter<NopFilter>();

// The above code will generate the following code:

sealed class Command0Invoker(string[] args, Action command) : ConsoleAppFilter(null!)
{
    public ConsoleAppFilter BuildFilter()
    {
        var filter0 = new NopFilter(this);
        var filter1 = new NopFilter(filter0);
        var filter2 = new NopFilter(filter1);
        var filter3 = new NopFilter(filter2);
        var filter4 = new NopFilter(filter3);
        return filter4;
    }

    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        return RunCommand0Async(context.Arguments, args, command, context, cancellationToken);
    }
}
```

When an `async Task` completes synchronously, it returns the equivalent of `Task.CompletedTask`, so `ValueTask` is not necessary.

Dependency Injection(Logging, Configuration, etc...)
---
The execution processing of `ConsoleAppFramework` fully supports `DI`. When you want to use a logger, read a configuration, or share processing with an ASP.NET project, using `Microsoft.Extensions.DependencyInjection` or other DI libraries can make processing convenient.

Lambda expressions passed to Run, class constructors, methods, and filter constructors can inject services obtained from `IServiceProvider`. Let's look at a minimal example. Setting any `System.IServiceProvider` to `ConsoleApp.ServiceProvider` enables DI throughout the system.

```csharp
// Microsoft.Extensions.DependencyInjection
var services = new ServiceCollection();
services.AddTransient<MyService>();

using var serviceProvider = services.BuildServiceProvider();

// Any DI library can be used as long as it can create an IServiceProvider
ConsoleApp.ServiceProvider = serviceProvider;

// When passing to a lambda expression/method, using [FromServices] indicates that it is passed via DI, not as a parameter
ConsoleApp.Run(args, ([FromServices]MyService service, int x, int y) => Console.WriteLine(x + y));
```

When passing to a lambda expression or method, the `[FromServices]` attribute is used to distinguish it from command parameters. When passing a class, Constructor Injection can be used, resulting in a simpler appearance.

Let's try injecting a logger and enabling output to a file. The libraries used are Microsoft.Extensions.Logging and [Cysharp/ZLogger](https://github.com/Cysharp/ZLogger/) (a high-performance logger built on top of MS.E.Logging).


```csharp
// Package Import: ZLogger
var services = new ServiceCollection();
services.AddLogging(x =>
{
    x.ClearProviders();
    x.SetMinimumLevel(LogLevel.Trace);
    x.AddZLoggerConsole();
    x.AddZLoggerFile("log.txt");
});

using var serviceProvider = services.BuildServiceProvider(); // using for logger flush(important!)
ConsoleApp.ServiceProvider = serviceProvider;

var app = ConsoleApp.Create();
app.Add<MyCommand>();
app.Run(args);

// inject logger to constructor
public class MyCommand(ILogger<MyCommand> logger)
{
    [Command("")]
    public void Echo(string msg)
    {
        logger.ZLogInformation($"Message is {msg}");
    }
}
```

`ConsoleApp` has replaceable default logging methods `ConsoleApp.Log` and `ConsoleApp.LogError` used for Help display and exception handling. If using `ILogger<T>`, it's better to replace these as well.

```csharp
using var serviceProvider = services.BuildServiceProvider(); // using for cleanup(important)
ConsoleApp.ServiceProvider = serviceProvider;

// setup ConsoleApp system logger
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
ConsoleApp.Log = msg => logger.LogInformation(msg);
ConsoleApp.LogError = msg => logger.LogError(msg);
```

DI can also be effectively used when reading application configuration from `appsettings.json`. For example, suppose you have the following JSON file.

```json
{
  "Position": {
    "Title": "Editor",
    "Name": "Joe Smith"
  },
  "MyKey": "My appsettings.json Value",
  "AllowedHosts": "*"
}
```

Using `Microsoft.Extensions.Configuration.Json`, reading, binding, and registering with DI can be done as follows.

```csharp
// Package Import: Microsoft.Extensions.Configuration.Json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

// Bind to services
var services = new ServiceCollection();
services.Configure<PositionOptions>(configuration.GetSection("Position"));

using var serviceProvider = services.BuildServiceProvider();
ConsoleApp.ServiceProvider = serviceProvider;

var app = ConsoleApp.Create();
app.Add<MyCommand>();
app.Run(args);

// inject options
public class MyCommand(IOptions<PositionOptions> options)
{
    [Command("")]
    public void Echo(string msg)
    {
        ConsoleApp.Log($"Binded Option: {options.Value.Title} {options.Value.Name}");
    }
}

public class PositionOptions
{
    public string Title { get; set; } = "";
    public string Name { get; set; } = "";
}
```

If you have other applications such as ASP.NET in the entire project and want to use common DI and configuration set up using `Microsoft.Extensions.Hosting`, you can share them by setting the `IServiceProvider` of `IHost` after building.

```csharp
// Package Import: Microsoft.Extensions.Hosting
var builder = Host.CreateApplicationBuilder(); // don't pass args.

using var host = builder.Build(); // using
ConsoleApp.ServiceProvider = host.Services; // use host ServiceProvider

ConsoleApp.Run(args, ([FromServices] ILogger<Program> logger) => logger.LogInformation("Hello World!"));
```

ConsoleAppFramework has its own lifetime management (see the [CancellationToken(Gracefully Shutdown) and Timeout](#cancellationtokengracefully-shutdown-and-timeout) section), so Host's Start/Stop is not necessary. However, be sure to use the Host itself.

As it is, the DI scope is not set, but by using a global filter, you can add a scope for each command execution. `ConsoleAppFilter` can also inject services via constructor injection, so let's get the `IServiceProvider`.

```csharp
var app = ConsoleApp.Create();
app.UseFilter<ServiceProviderScopeFilter>();

internal class ServiceProviderScopeFilter(IServiceProvider serviceProvider, ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        // create Microsoft.Extensions.DependencyInjection scope
        await using var scope = serviceProvider.CreateAsyncScope();
        await Next.InvokeAsync(context, cancellationToken);
    }
}
```

Publish to executable file
---

* Native AOT
* dotnet run
* dotnet publish

License
---
This library is under the MIT License.
