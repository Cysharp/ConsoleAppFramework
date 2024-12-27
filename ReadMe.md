ConsoleAppFramework
===
[![GitHub Actions](https://github.com/Cysharp/ConsoleAppFramework/workflows/Build-Debug/badge.svg)](https://github.com/Cysharp/ConsoleAppFramework/actions) [![Releases](https://img.shields.io/github/release/Cysharp/ConsoleAppFramework.svg)](https://github.com/Cysharp/ConsoleAppFramework/releases)

ConsoleAppFramework v5 is Zero Dependency, Zero Overhead, Zero Reflection, Zero Allocation, AOT Safe CLI Framework powered by C# Source Generator; achieves exceptionally high performance, fastest start-up time(with NativeAOT) and minimal binary size. Leveraging the latest features of .NET 8 and C# 12 ([IncrementalGenerator](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md), [managed function pointer](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/function-pointers#function-pointers-1), [params arrays and default values lambda expression](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions#input-parameters-of-a-lambda-expression), [`ISpanParsable<T>`](https://learn.microsoft.com/en-us/dotnet/api/system.ispanparsable-1), [`PosixSignalRegistration`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.posixsignalregistration), etc.), this library ensures maximum performance while maintaining flexibility and extensibility.

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
internal static partial class ConsoleApp
{
    // Generate the Run method itself with arguments and body to match the lambda expression
    public static void Run(string[] args, Action<int, int> command)
    {
        // code body
    }
}
```

<details><summary>Full generated source code</summary>

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
                        if (!TryIncrementIndex(ref i, args.Length) || !int.TryParse(args[i], out arg0)) { ThrowArgumentParseFailed("foo", args[i]); }
                        arg0Parsed = true;
                        break;
                    }
                    case "--bar":
                    {
                        if (!TryIncrementIndex(ref i, args.Length) || !int.TryParse(args[i], out arg1)) { ThrowArgumentParseFailed("bar", args[i]); }
                        arg1Parsed = true;
                        break;
                    }
                    default:
                        if (string.Equals(name, "--foo", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!TryIncrementIndex(ref i, args.Length) || !int.TryParse(args[i], out arg0)) { ThrowArgumentParseFailed("foo", args[i]); }
                            arg0Parsed = true;
                            break;
                        }
                        if (string.Equals(name, "--bar", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!TryIncrementIndex(ref i, args.Length) || !int.TryParse(args[i], out arg1)) { ThrowArgumentParseFailed("bar", args[i]); }
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
            if (ex is ValidationException or ArgumentParseFailedException)
            {
                LogError(ex.Message);
            }
            else
            {
                LogError(ex.ToString());
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryIncrementIndex(ref int index, int length)
    {
        if (index < length)
        {
            index++;
            return true;
        }
        return false;
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
</details>

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

> dotnet add package [ConsoleAppFramework](https://www.nuget.org/packages/ConsoleAppFramework)

ConsoleAppFramework is an analyzer (Source Generator) and does not have any dll references. When referenced, the entry point class `ConsoleAppFramework.ConsoleApp` is generated internally.

The first argument of `Run` or `RunAsync` can be `string[] args`, and the second argument can be any lambda expression, method, or function reference. Based on the content of the second argument, the corresponding function is automatically generated.

```csharp
using ConsoleAppFramework;

ConsoleApp.Run(args, (string name) => Console.WriteLine($"Hello {name}"));
```

> The latest Visual Studio changed the execution timing of Source Generators to either during save or at compile time. If you encounter unexpected behavior, try compiling once or change the option to "Automatic" under TextEditor -> C# -> Advanced -> Source Generators.

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

In addition to `-h|--help`, there is another special built-in option: `--version`. In default, it displays the `AssemblyInformationalVersion` without source revision or `AssemblyVersion`. You can configure version string by `ConsoleApp.Version`, for example `ConsoleApp.Version = "2001.9.3f14-preview2";`.

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

### Register from attribute

Instead of using `Add<T>`, you can automatically add commands by applying the `[RegisterCommands]` attribute to a class.

```csharp
[RegisterCommands]
public class Foo
{
    public void Baz(int x)
    {
        Console.Write(x);
    }
}

[RegisterCommands("bar")]
public class Bar
{
    public void Baz(int x)
    {
        Console.Write(x);
    }
}
```

These are automatically added when using `ConsoleApp.Create()`.

```csharp
var app = ConsoleApp.Create();

// Commands:
//   baz
//   bar baz
app.Run(args);
```

You can also combine this with `Add` or `Add<T>` to add more commands.

### Performance of Commands

In `ConsoleAppFramework`, the number and types of registered commands are statically determined at compile time. For example, let's register the following four commands:

```csharp
app.Add("foo", () => { });
app.Add("foo bar", (int x, int y) => { });
app.Add("foo bar barbaz", (DateTime dateTime) => { });
app.Add("foo baz", async (string foo = "test", CancellationToken cancellationToken = default) => { });
```

The Source Generator generates four fields and holds them with specific types.

```csharp
partial class ConsoleAppBuilder
{
    Action command0 = default!;
    Action<int, int> command1 = default!;
    Action<global::System.DateTime> command2 = default!;
    Func<string, global::System.Threading.CancellationToken, Task> command3 = default!;

    partial void AddCore(string commandName, Delegate command)
    {
        switch (commandName)
        {
            case "foo":
                this.command0 = Unsafe.As<Action>(command);
                break;
            case "foo bar":
                this.command1 = Unsafe.As<Action<int, int>>(command);
                break;
            case "foo bar barbaz":
                this.command2 = Unsafe.As<Action<global::System.DateTime>>(command);
                break;
            case "foo baz":
                this.command3 = Unsafe.As<Func<string, global::System.Threading.CancellationToken, Task>>(command);
                break;
            default:
                break;
        }
    }
}
```

This ensures the fastest execution speed without any additional unnecessary allocations such as arrays and without any boxing since it holds static delegate types.

Command routing also generates a switch of nested string constants.

```csharp
partial void RunCore(string[] args)
{
    if (args.Length == 0)
    {
        ShowHelp(-1);
        return;
    }
    switch (args[0])
    {
        case "foo":
            if (args.Length == 1)
            {
                RunCommand0(args, args.AsSpan(1), command0);
                return;
            }
            switch (args[1])
            {
                case "bar":
                    if (args.Length == 2)
                    {
                        RunCommand1(args, args.AsSpan(2), command1);
                        return;
                    }
                    switch (args[2])
                    {
                        case "barbaz":
                            RunCommand2(args, args.AsSpan(3), command2);
                            break;
                        default:
                            RunCommand1(args, args.AsSpan(2), command1);
                            break;
                    }
                    break;
                case "baz":
                    RunCommand3(args, args.AsSpan(2), command3);
                    break;
                default:
                    RunCommand0(args, args.AsSpan(1), command0);
                    break;
            }
            break;
        default:
            ShowHelp(-1);
            break;
    }
}
```

The C# compiler performs complex generation for string constant switches, making them extremely fast, and it would be difficult to achieve faster routing than this.

Disable Naming Conversion
---
Command names and option names are automatically converted to kebab-case by default. While this follows standard command-line tool naming conventions, you might find this conversion inconvenient when creating batch files for internal applications. Therefore, it's possible to disable this conversion at the assembly level.

```csharp
using ConsoleAppFramework;

[assembly: ConsoleAppFrameworkGeneratorOptions(DisableNamingConversion = true)]

var app = ConsoleApp.Create();
app.Add<MyProjectCommand>();
app.Run(args);

public class MyProjectCommand
{
    public void ExecuteCommand(string fooBarBaz)
    {
        Console.WriteLine(fooBarBaz);
    }
}
```

You can disable automatic conversion by using `[assembly: ConsoleAppFrameworkGeneratorOptions(DisableNamingConversion = true)]`. In this case, the command would be `ExecuteCommand --fooBarBaz`.

Parse and Value Binding
---
The method parameter names and types determine how to parse and bind values from the command-line arguments. When using lambda expressions, optional values and `params` arrays supported from C# 12 are also supported.

```csharp
ConsoleApp.Run(args, (
    [Argument]DateTime dateTime,  // Argument
    [Argument]Guid guidvalue,     // 
    int intVar,                   // required
    bool boolFlag,                // flag
    MyEnum enumValue,             // enum
    int[] array,                  // array
    MyClass obj,                  // object
    string optional = "abcde",    // optional
    double? nullableValue = null, // nullable
    params string[] paramsArray   // params
    ) => { });
```    

When using `ConsoleApp.Run`, you can check the syntax of the command line in the tooltip to see how it is generated.

![image](https://github.com/Cysharp/ConsoleAppFramework/assets/46207/af480566-adac-4767-bd5e-af89ab6d71f1)

For the rules on converting parameter names to option names, aliases, and how to set documentation, refer to the [Option aliases](#option-aliases-and-help-version) section.

Parameters marked with the `[Argument]` attribute receive values in order without parameter names. This attribute can only be set on sequential parameters from the beginning.

To convert from string arguments to various types, basic primitive types (`string`, `char`, `sbyte`, `byte`, `short`, `int`, `long`, `uint`, `ushort`, `ulong`, `decimal`, `float`, `double`) use `TryParse`. For types that implement `ISpanParsable<T>` (`DateTime`, `DateTimeOffset`, `Guid`, `BigInteger`, `Complex`, `Half`, `Int128`, etc.), [IParsable<TSelf>.TryParse](https://learn.microsoft.com/en-us/dotnet/api/system.iparsable-1.tryparse?view=net-8.0#system-ispanparsable-1-tryparse(system-readonlyspan((system-char))-system-iformatprovider-0@)) or [ISpanParsable<TSelf>.TryParse](https://learn.microsoft.com/en-us/dotnet/api/system.ispanparsable-1.tryparse?view=net-8.0#system-ispanparsable-1-tryparse(system-readonlyspan((system-char))-system-iformatprovider-0@)) is used.

For `enum`, it is parsed using `Enum.TryParse(ignoreCase: true)`.

`bool` is treated as a flag and is always optional. It becomes `true` when the parameter name is passed.

### Array

Array parsing has three special patterns.

For a regular `T[]`, if the value starts with `[`, it is parsed using `JsonSerializer.Deserialize`. Otherwise, it is parsed as comma-separated values. For example, `[1,2,3]` or `1,2,3` are allowed as values. To set an empty array, pass `[]`.

For `params T[]`, all subsequent arguments become the values of the array. For example, if there is an input like `--paramsArray foo bar baz`, it will be bound to a value like `["foo", "bar", "baz"]`.

### Object

If none of the above cases apply, `JsonSerializer.Deserialize<T>` is used to perform binding as JSON. However, `CancellationToken` and `ConsoleAppContext` are treated as special types and excluded from binding. Also, parameters with the `[FromServices]` attribute are not subject to binding.

If you want to change the deserialization options, you can set `JsonSerializerOptions` to `ConsoleApp.JsonSerializerOptions`.

> NOTE: If they are not set when NativeAOT is used, a runtime exception may occur. If they are included in the parsing process, be sure to set source generated options.

### Custom Value Converter

To perform custom binding to existing types that do not support `ISpanParsable<T>`, you can create and set up a custom parser. For example, if you want to pass `System.Numerics.Vector3` as a comma-separated string like `1.3,4.12,5.947` and parse it, you can create an `Attribute` with `AttributeTargets.Parameter` that implements `IArgumentParser<T>`'s `static bool TryParse(ReadOnlySpan<char> s, out Vector3 result)` as follows:

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

By setting this attribute on a parameter, the custom parser will be called when parsing the args.

```csharp
ConsoleApp.Run(args, ([Vector3Parser] Vector3 position) => Console.WriteLine(position));
```

### Syntax Parsing Policy and Performance

While there are some standards for command-line arguments, such as UNIX tools and POSIX, there is no absolute specification. The [Command-line syntax overview for System.CommandLine](https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax) provides an explanation of the specifications adopted by System.CommandLine. However, ConsoleAppFramework, while referring to these specifications to some extent, does not necessarily aim to fully comply with them.

For example, specifications that change behavior based on `-x` and `-X` or allow bundling `-f -d -x` as `-fdx` are not easy to understand and also take time to parse. The poor performance of System.CommandLine may be influenced by its adherence to complex grammar. Therefore, ConsoleAppFramework prioritizes performance and clear rules. It uses lower-kebab-case as the basis while allowing case-insensitive matching. It does not support ambiguous grammar that cannot be processed in a single pass or takes time to parse.

[System.CommandLine seems to be aiming for a new direction in .NET 9 and .NET 10](https://github.com/dotnet/command-line-api/issues/2338), but from a performance perspective, it will never surpass ConsoleAppFramework.

CancellationToken(Gracefully Shutdown) and Timeout
---
In ConsoleAppFramework, when you pass a `CancellationToken` as an argument, it can be used to check for interruption commands (SIGINT/SIGTERM/SIGKILL - Ctrl+C) rather than being treated as a parameter. For handling this, ConsoleAppFramework performs special code generation when a `CancellationToken` is included in the parameters.

```csharp
using var posixSignalHandler = PosixSignalHandler.Register(ConsoleApp.Timeout);
var arg0 = posixSignalHandler.Token;

await Task.Run(() => command(arg0!)).WaitAsync(posixSignalHandler.TimeoutToken);
```

If a CancellationToken is not passed, the application is immediately forced to terminate when an interruption command (Ctrl+C) is received. However, if a CancellationToken is present, it internally uses [`PosixSignalRegistration`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.posixsignalregistration) to hook SIGINT/SIGTERM/SIGKILL and sets the CancellationToken to a canceled state. Additionally, it prevents forced termination to allow for a graceful shutdown.

If the CancellationToken is handled correctly, the application can perform proper termination processing based on the application's handling. However, if the CancellationToken is mishandled, the application may not terminate even when an interruption command is received. To avoid this, a timeout timer starts after the interruption command, and the application is forcibly terminated again after the specified time.

The default timeout is 5 seconds, but it can be changed using `ConsoleApp.Timeout`. For example, setting it to `ConsoleApp.Timeout = Timeout.InfiniteTimeSpan;` disables the forced termination caused by the timeout.

The hooking behavior using `PosixSignalRegistration` is determined by the presence of a `CancellationToken` (or always takes effect if a filter is set). Therefore, even for synchronous methods, it is possible to change the behavior by including a `CancellationToken` as an argument.

Exit Code
---
If the method returns `int` or `Task<int>`, `ConsoleAppFramework` will set the return value to the exit code. Due to the nature of code generation, when writing lambda expressions, you need to explicitly specify either `int` or `Task<int>`.

```csharp
// return Random ExitCode...
ConsoleApp.Run(args, int () => Random.Shared.Next());
```

```csharp
// return StatusCode
await ConsoleApp.RunAsync(args, async Task<int> (string url, CancellationToken cancellationToken) =>
{
    using var client = new HttpClient();
    var response = await client.GetAsync(url, cancellationToken);
    return (int)response.StatusCode;
});
```

If the method throws an unhandled exception, ConsoleAppFramework always set `1` to the exit code. Also, in that case, output `Exception.ToString` to `ConsoleApp.LogError` (the default is `Console.WriteLine`). If you want to modify this code, please create a custom filter. For more details, refer to the [Filter](#filtermiddleware-pipline--consoleappcontext) section. 

Attribute based parameters validation
---
`ConsoleAppFramework` performs validation when the parameters are marked with attributes for validation from `System.ComponentModel.DataAnnotations` (more precisely, attributes that implement `ValidationAttribute`). The validation occurs after parameter binding and before command execution. If the validation fails, it throws a `ValidationException`.

```csharp
ConsoleApp.Run(args, ([EmailAddress] string firstArg, [Range(0, 2)] int secondArg) => { });
```

For example, if you pass arguments like `args = "--first-arg invalid.email --second-arg 10".Split(' ');`, you will see validation failure messages such as:

```txt
The firstArg field is not a valid e-mail address.
The field secondArg must be between 0 and 2.
```

By default, the ExitCode is set to 1 in this case.

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

`ConsoleAppFilter` is defined as `internal` for each project by the Source Generator. Therefore, an additional library is provided for referencing common filter definitions across projects.

> PM> Install-Package [ConsoleAppFramework.Abstractions](https://www.nuget.org/packages/ConsoleAppFramework.Abstractions)

This library includes the following classes:

* `IArgumentParser<T>`
* `ConsoleAppContext`
* `ConsoleAppFilter`
* `ConsoleAppFilterAttribute<T>`

Internally, when referencing `ConsoleAppFramework.Abstractions`, the `USE_EXTERNAL_CONSOLEAPP_ABSTRACTIONS` compilation symbol is added. This disables the above classes generated by the Source Generator, and prioritizes using the classes within the library.

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

If you are referencing `Microsoft.Extensions.DependencyInjection`, you can call the `ConfigureServices` method from `ConsoleApp.ConsoleAppBuilder` (ConsoleAppFramework adds methods based on your project's reference status).

```csharp
var app = ConsoleApp.Create()
    .ConfigureServices(service =>
    {
        service.AddTransient<MyService>();
    });

app.Add("", ([FromServices] MyService service, int x, int y) => Console.WriteLine(x + y));

app.Run(args);
```

When passing to a lambda expression or method, the `[FromServices]` attribute is used to distinguish it from command parameters. When passing a class, Constructor Injection can be used, resulting in a simpler appearance.

Let's try injecting a logger and enabling output to a file. The libraries used are Microsoft.Extensions.Logging and [Cysharp/ZLogger](https://github.com/Cysharp/ZLogger/) (a high-performance logger built on top of MS.E.Logging). If you are referencing `Microsoft.Extensions.Logging`, you can call `ConfigureLogging` from `ConsoleAppBuilder`.

```csharp
// Package Import: ZLogger
var app = ConsoleApp.Create()
    .ConfigureLogging(x =>
    {
        x.ClearProviders();
        x.SetMinimumLevel(LogLevel.Trace);
        x.AddZLoggerConsole();
        x.AddZLoggerFile("log.txt");
    });

app.Add<MyCommand>();
app.Run(args);

// inject logger to constructor
public class MyCommand(ILogger<MyCommand> logger)
{
    public void Echo(string msg)
    {
        logger.ZLogInformation($"Message is {msg}");
    }
}
```

For building an `IServiceProvider`, `ConfigureServices/ConfigureLogging` uses `Microsoft.Extensions.DependencyInjection.ServiceCollection`. If you want to set a custom ServiceProvider or a ServiceProvider built from Host, or if you want to execute DI with `ConsoleApp.Run`, set it to `ConsoleApp.ServiceProvider`.

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

`ConsoleApp` has replaceable default logging methods `ConsoleApp.Log` and `ConsoleApp.LogError` used for Help display and exception handling. If using `ILogger<T>`, it's better to replace these as well.

```csharp
app.UseFilter<ReplaceLogFilter>();

// inject logger to filter
internal sealed class ReplaceLogFilter(ConsoleAppFilter next, ILogger<Program> logger)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        ConsoleApp.Log = msg => logger.LogInformation(msg);
        ConsoleApp.LogError = msg => logger.LogError(msg);

        return Next.InvokeAsync(context, cancellationToken);
    }
}
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

```xml
<ItemGroup>
    <None Update="appsettings.json">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

Using `Microsoft.Extensions.Configuration.Json`, reading, binding, and registering with DI can be done as follows.

```csharp
// Package Import: Microsoft.Extensions.Configuration.Json
var app = ConsoleApp.Create()
    .ConfigureDefaultConfiguration()
    .ConfigureServices((configuration, services) =>
    {
        // Package Import: Microsoft.Extensions.Options.ConfigurationExtensions
        services.Configure<PositionOptions>(configuration.GetSection("Position"));
    });

app.Add<MyCommand>();
app.Run(args);

// inject options
public class MyCommand(IOptions<PositionOptions> options)
{
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

When `Microsoft.Extensions.Configuration` is imported, `ConfigureEmptyConfiguration` becomes available to call. Additionally, when `Microsoft.Extensions.Configuration.Json` is imported, `ConfigureDefaultConfiguration` becomes available to call. In DefaultConfiguration, `SetBasePath(System.IO.Directory.GetCurrentDirectory())` and `AddJsonFile("appsettings.json", optional: true)` are executed before calling `Action<IConfigurationBuilder> configure`.

Furthermore, overloads of `Action<IConfiguration, IServiceCollection> configure` and `Action<IConfiguration, ILoggingBuilder> configure` are added to `ConfigureServices` and `ConfigureLogging`, allowing you to retrieve the Configuration when executing the delegate.

without Hosting dependency, I've prefere these import packages.

```xml
<ItemGroup>
	<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
	<PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="9.0.0" />
	<PackageReference Include="ZLogger" Version="2.5.9" />
</ItemGroup>
```

As it is, the DI scope is not set, but by using a global filter, you can add a scope for each command execution. `ConsoleAppFilter` can also inject services via constructor injection, so let's get the `IServiceProvider`.

```csharp
app.UseFilter<ServiceProviderScopeFilter>();

internal class ServiceProviderScopeFilter(IServiceProvider serviceProvider, ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        // create Microsoft.Extensions.DependencyInjection scope
        await using var scope = serviceProvider.CreateAsyncScope();

        var originalServiceProvider = ConsoleApp.ServiceProvider;
        ConsoleApp.ServiceProvider = scope.ServiceProvider;
        try
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        finally
        {
            ConsoleApp.ServiceProvider = originalServiceProvider;
        }
    }
}
```

However, since the construction of the filters is performed before execution, automatic injection using scopes is only effective for the command body itself.

If you have other applications such as ASP.NET in the entire project and want to use common DI and configuration set up using `Microsoft.Extensions.Hosting`, you can call `ToConsoleAppBuilder` from `IHostBuilder` or `HostApplicationBuilder`.

```csharp
// Package Import: Microsoft.Extensions.Hosting
var app = Host.CreateApplicationBuilder()
    .ToConsoleAppBuilder();
```

In this case, it builds the HostBuilder, creates a Scope for the ServiceProvider, and disposes of all of them after execution.

ConsoleAppFramework has its own lifetime management (see the [CancellationToken(Gracefully Shutdown) and Timeout](#cancellationtokengracefully-shutdown-and-timeout) section), so Host's Start/Stop is not necessary.

Colorize
---
The framework doesn't support colorization directly; however, utilities like [Cysharp/Kokuban](https://github.com/Cysharp/Kokuban) make console colorization easy.

Publish to executable file
---
There are multiple ways to run a CLI application in .NET:

* [dotnet run](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run)
* [dotnet build](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build)
* [dotnet publish](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish)

`run` is convenient when you want to execute the `csproj` directly, such as for starting command tools in CI. `build` and `publish` are quite similar, so it's possible to discuss them in general terms, but it's a bit difficult to talk about the precise differences. For more details, it's a good idea to check out [`build` vs `publish` -- can they be friends? · Issue #26247 · dotnet/sdk](https://github.com/dotnet/sdk/issues/26247).

Also, to run with Native AOT, please refer to the [Native AOT deployment overview](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/). In any case, ConsoleAppFramework thoroughly implements a dependency-free and reflection-free approach, so it shouldn't be an obstacle to execution.

v4 -> v5 Migration Guide
---
v4 was running on top of `Microsoft.Extensions.Hosting`, so build a Host in the same way and set up a ServiceProvider.

```csharp
using var host = Host.CreateDefaultBuilder().Build(); // use using for host lifetime
using var scope = host.Services.CreateScope(); // create execution scope
ConsoleApp.ServiceProvider = scope.ServiceProvider;
```

* `var app = ConsoleApp.Create(args); app.Run();` -> `var app = ConsoleApp.Create(); app.Run(args);`
* `app.AddCommand/AddSubCommand` -> `app.Add(string commandName)`
* `app.AddRootCommand` -> `app.Add("")`
* `app.AddCommands<T>` -> `app.Add<T>`
* `app.AddSubCommands<T>` -> `app.Add<T>(string commandPath)`
* `app.AddAllCommandType` -> `NotSupported`(use `Add<T>` manually)
* `[Option(int index)]` -> `[Argument]`
* `[Option(string shortName, string description)]` -> `Xml Document Comment`
* `ConsoleAppFilter.Order` -> `NotSupported`(global -> class -> method declrative order)
* `ConsoleAppOptions.GlobalFilters` -> `app.UseFilter<T>`
* `ConsoleAppBase` -> inject `ConsoleAppContext`, `CancellationToken` to method

License
---
This library is under the MIT License.
