ConsoleAppFramework
===
[![GitHub Actions](https://github.com/Cysharp/ConsoleAppFramework/workflows/Build-Debug/badge.svg)](https://github.com/Cysharp/ConsoleAppFramework/actions) [![Releases](https://img.shields.io/github/release/Cysharp/ConsoleAppFramework.svg)](https://github.com/Cysharp/ConsoleAppFramework/releases)

ConsoleAppFramework v5 is Zero Dependency, Zero Overhead, Zero Reflection, Zero Allocation, AOT Safe CLI Framework powered by C# Source Generator. Leveraging the latest features of .NET 8 and C# 12 ([IncrementalGenerator](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md), [managed function pointer](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/function-pointers#function-pointers-1), [params arrays and default values lambda expression](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions#input-parameters-of-a-lambda-expression), [`ISpanParsable<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.ispanparsable-1), [`PosixSignalRegistration`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.posixsignalregistration), etc.), this library ensures maximum performance while maintaining flexibility and extensibility.

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
* High performance value parsing via `ISpanParsable<T>`
* Parsing of params arrays
* Parsing of JSON arguments
* Help(`-h|--help`) option builder
* Default show version(`--version`) option

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
    * For example, `XmlReader` becomes `xml-reader`
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

You can create a deep command hierarchy by adding commands with paths separated by `/` when registering them. This allows you to add commands at nested levels.

```csharp
var app = ConsoleApp.Create();

app.Add("foo", () => { });
app.Add("foo/bar", () => { });
app.Add("foo/bar/barbaz", () => { });
app.Add("foo/baz", () => { });

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



Log
---


Attribute based parameters validation
---






Filter(Middleware) Pipline
---

// TODO:samples? change exit code, log, etc...
// TODO: how to share filter

```csharp
internal class TimestampFilter(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("filter1");
        return Next.InvokeAsync(cancellationToken);
    }
}


internal class LogExecutionTimeFilter(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(CancellationToken cancellationToken)
    {
        var startingTime = Stopwatch.GetTimestamp();
        try
        {
            await Next.InvokeAsync(cancellationToken);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(startingTime);
            ConsoleApp.Log($"Execution Time: {elapsed}");
        }
    }
}
```




Dependency Injection(Logging, Configuration, etc...)
---

`[FromServices]`

// TODO: minimum single context

```csharp
public class FilterContext : IServiceProvider
{
    public long Timestamp { get; set; }
    public Guid UserId { get; set; }

    object IServiceProvider.GetService(Type serviceType)
    {
        if (serviceType == typeof(FilterContext)) return this;
        throw new InvalidOperationException("Type is invalid:" + serviceType);
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
