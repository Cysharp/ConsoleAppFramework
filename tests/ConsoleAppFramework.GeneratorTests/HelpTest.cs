using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFramework.GeneratorTests;

public class HelpTest
{
    VerifyHelper verifier = new VerifyHelper("CAF");

    [Test]
    public async Task Version()
    {
        var version = GetEntryAssemblyVersion();

        await verifier.Execute(code: $$"""
ConsoleApp.Log = x => Console.WriteLine(x);
ConsoleApp.Run(args, (int x, int y) => { });
""",
    args: "--version",
    expected: $$"""
{{version}}

""");
        // custom
        await verifier.Execute(code: $$"""
ConsoleApp.Log = x => Console.WriteLine(x);
ConsoleApp.Version = "9999.9999999abcdefg";
ConsoleApp.Run(args, (int x, int y) => { });
""",
   args: "--version",
   expected: """
9999.9999999abcdefg

""");
    }

    [Test]
    public async Task VersionOnBuilder()
    {
        var version = GetEntryAssemblyVersion();

        await verifier.Execute(code: """
ConsoleApp.Log = x => Console.WriteLine(x);
var app = ConsoleApp.Create();
app.Run(args);
""",
    args: "--version",
    expected: $$"""
{{version}}

""");
    }

    [Test]
    public async Task Run()
    {
        await verifier.Execute(code: """
ConsoleApp.Log = x => Console.WriteLine(x);
ConsoleApp.Run(args, (int x, int y) => { });
""",
            args: "--help",
            expected: """
Usage: [options...] [-h|--help] [--version]

Options:
  --x <int>     [Required]
  --y <int>     [Required]

""");
    }

    [Test]
    public async Task RunVoid()
    {
        await verifier.Execute(code: """
ConsoleApp.Log = x => Console.WriteLine(x);
ConsoleApp.Run(args, () => { });
""",
            args: "--help",
            expected: """
Usage: [-h|--help] [--version]

""");
    }

    [Test]
    public async Task RootOnly()
    {
        await verifier.Execute(code: """
ConsoleApp.Log = x => Console.WriteLine(x);
var app = ConsoleApp.Create();
app.Add("", (int x, int y) => { });
app.Run(args);
""",
    args: "--help",
    expected: """
Usage: [options...] [-h|--help] [--version]

Options:
  --x <int>     [Required]
  --y <int>     [Required]

""");
    }

    [Test]
    public async Task ListWithoutRoot()
    {
        var code = """
ConsoleApp.Log = x => Console.WriteLine(x);
var app = ConsoleApp.Create();
app.Add("a", (int x, int y) => { });
app.Add("ab", (int x, int y) => { });
app.Add("a b c", (int x, int y) => { });
app.Run(args);
""";
        await verifier.Execute(code, args: "--help", expected: """
Usage: [command] [-h|--help] [--version]

Commands:
  a
  a b c
  ab

""");
    }

    [Test]
    public async Task ListWithRoot()
    {
        var code = """
ConsoleApp.Log = x => Console.WriteLine(x);
var app = ConsoleApp.Create();
app.Add("", (int x, int y) => { });
app.Add("a", (int x, int y) => { });
app.Add("ab", (int x, int y) => { });
app.Add("a b c", (int x, int y) => { });
app.Run(args);
""";
        await verifier.Execute(code, args: "--help", expected: """
Usage: [command] [options...] [-h|--help] [--version]

Options:
  --x <int>     [Required]
  --y <int>     [Required]

Commands:
  a
  a b c
  ab

""");
    }

    [Test]
    public async Task NoArgsOnRootShowsSameHelpTextAsHelpWhenParametersAreRequired()
    {
        var code = """
ConsoleApp.Log = x => Console.WriteLine(x);
var app = ConsoleApp.Create();
app.Add("", (int x, int y) => { });
app.Add("a", (int x, int y) => { });
app.Add("ab", (int x, int y) => { });
app.Add("a b c", (int x, int y) => { });
app.Run(args);
""";
        var noArgsOutput = verifier.Error(code, "");
        var helpOutput = verifier.Error(code, "--help");

        await Assert.That(noArgsOutput).IsEqualTo(helpOutput);
    }

    [Test]
    public async Task SelectLeafHelp()
    {
        var code = """
ConsoleApp.Log = x => Console.WriteLine(x);
var app = ConsoleApp.Create();
app.Add("", (int x, int y) => { });
app.Add("a", (int x, int y) => { });
app.Add("ab", (int x, int y) => { });
app.Add("a b c", (int x, int y) => { });
app.Run(args);
""";
        await verifier.Execute(code, args: "a b c --help", expected: """
Usage: a b c [options...] [-h|--help] [--version]

Options:
  --x <int>     [Required]
  --y <int>     [Required]

""");
    }

    [Test]
    public async Task Summary()
    {
        var code = """
ConsoleApp.Log = x => Console.WriteLine(x);
var app = ConsoleApp.Create();
app.Add<MyClass>();
app.Run(args);

public class MyClass
{
    /// <summary>
    /// hello my world.
    /// </summary>
    /// <param name="fooBar">-f|-fb, my foo is not bar.</param>
    public async Task HelloWorld(string fooBar)
    {
        Console.Write("Hello World! " + fooBar);
    }
}
""";
        await verifier.Execute(code, args: "--help", expected: """
Usage: [command] [-h|--help] [--version]

Commands:
  hello-world    hello my world.

""");

        await verifier.Execute(code, args: "hello-world --help", expected: """
Usage: hello-world [options...] [-h|--help] [--version]

hello my world.

Options:
  -f, -fb, --foo-bar <string>    my foo is not bar. [Required]

""");
    }

    [Test]
    public async Task ArgumentOnly()
    {
        await verifier.Execute(code: """
ConsoleApp.Log = x => Console.WriteLine(x);
ConsoleApp.Run(args, ([Argument]int x, [Argument]int y) => { });
""",
            args: "--help",
            expected: """
Usage: [arguments...] [-h|--help] [--version]

Arguments:
  [0] <int>
  [1] <int>

""");
    }

    [Test]
    public async Task ArgumentWithParams()
    {
        await verifier.Execute(code: """
ConsoleApp.Log = x => Console.WriteLine(x);
ConsoleApp.Run(args, ([Argument]int x, [Argument]int y, params string[] yyy) => { });
""",
            args: "--help",
            expected: """
Usage: [arguments...] [options...] [-h|--help] [--version]

Arguments:
  [0] <int>
  [1] <int>

Options:
  --yyy <string[]>...    

""");
    }

    // Params

    [Test]
    public async Task Nullable()
    {
        await verifier.Execute(code: """
ConsoleApp.Log = x => Console.WriteLine(x);
ConsoleApp.Run(args, (int? x = null, string? y = null) => { });
""",
            args: "--help",
            expected: """
Usage: [options...] [-h|--help] [--version]

Options:
  --x <int?>        [Default: null]
  --y <string?>     [Default: null]

""");
    }

    [Test]
    public async Task EnumTest()
    {
        await verifier.Execute(code: """
ConsoleApp.Log = x => Console.WriteLine(x);
ConsoleApp.Run(args, (Fruit myFruit = Fruit.Apple, Fruit? moreFruit = null) => { });

enum Fruit
{
    Orange, Grape, Apple
}
""",
            args: "--help",
            expected: """
Usage: [options...] [-h|--help] [--version]

Options:
  --my-fruit <Fruit>        [Default: Apple]
  --more-fruit <Fruit?>     [Default: null]

""");
    }

    [Test]
    public async Task Summary2()
    {
        var code = """
ConsoleApp.Log = x => Console.WriteLine(x);
var app = ConsoleApp.Create();
app.Add<MyClass>();
app.Run(args);

public class MyClass
{
    /// <summary>
    /// hello my world.
    /// </summary>
    /// <param name="boo">-b, my boo is not boo.</param>
    /// <param name="fooBar">-f|-fb, my foo, is not bar.</param>
    public async Task HelloWorld([Argument]int boo, string fooBar)
    {
        Console.Write("Hello World! " + fooBar);
    }
}
""";
        await verifier.Execute(code, args: "hello-world --help", expected: """
Usage: hello-world [arguments...] [options...] [-h|--help] [--version]

hello my world.

Arguments:
  [0] <int>    my boo is not boo.

Options:
  -f, -fb, --foo-bar <string>    my foo, is not bar. [Required]

""");
    }

    [Test]
    public async Task HideDefaultValue()
    {
        var code = """
ConsoleApp.Log = x => Console.WriteLine(x);
ConsoleApp.Run(args, Commands.Hello);

static class Commands
{
    /// <summary>
    /// Display Hello.
    /// </summary>
    /// <param name="message">-m, Message to show.</param>
    public static void Hello([HideDefaultValue]string message = "ConsoleAppFramework") => Console.Write($"Hello, {message}");
}
""";
        await verifier.Execute(code, args: "--help", expected: """
Usage: [options...] [-h|--help] [--version]

Display Hello.

Options:
  -m, --message <string>    Message to show.

""");
    }

    [Test]
    public async Task GlobalOptions()
    {
        var code = """
ConsoleApp.Log = x => Console.WriteLine(x);
var app = ConsoleApp.Create();

app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    var p = builder.AddGlobalOption<int>("--parameter", "param global", defaultValue: 1000);
    var d = builder.AddGlobalOption<bool>(description: "run dry dry", name: "--dry-run");
    var r = builder.AddRequiredGlobalOption<int>("--p2|--p3", "param 2");
    return (p, d, r);
});

app.Add("", (int x, int y) => { });
app.Add("a", (int x, int y) => { });
app.Add("ab", (int x, int y) => { });
app.Add("a b c", (int x, int y) => { });
app.Run(args);
""";

        await verifier.Execute(code, args: "a --help", expected: """
Usage: a [options...] [-h|--help] [--version]

Options:
  --x <int>             [Required]
  --y <int>             [Required]
  --parameter <int>    param global [Default: 1000]
  --dry-run            run dry dry
  --p2, --p3 <int>     param 2 [Required]

""");
    }

    private static string GetEntryAssemblyVersion()
    {
        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (version == null)
        {
            return "1.0.0";
        }

        // Trim SourceRevisionId (SourceLink feature is enabled by default when using .NET SDK 8 or later)
        var i = version.IndexOf('+');
        if (i != -1)
        {
            version = version.Substring(0, i);
        }

        return version;
    }

    [Test]
    public async Task CommandAlias()
    {
        var code = """
ConsoleApp.Log = x => Console.WriteLine(x);
var app = ConsoleApp.Create();

app.Add("build|b", () => { Console.Write("build ok"); });
app.Add("test|t", () => { Console.Write("test ok");  });
app.Add<Commands>();

app.Run(args);

public class Commands
{
    /// <summary>Analyze the current package and report errors, but don't build object files.</summary>
    [Command("check|c")]
    public async Task Check() { Console.Write("check ok"); }

    /// <summary>Build this packages's and its dependencies' documenation.</summary>
    [Command("doc|d")]
    public async Task Doc() { Console.Write("doc ok"); }
}
""";

        await verifier.Execute(code, "--help", """
Usage: [command] [-h|--help] [--version]

Commands:
  build, b
  check, c    Analyze the current package and report errors, but don't build object files.
  doc, d      Build this packages's and its dependencies' documenation.
  test, t

""");
    }
}
