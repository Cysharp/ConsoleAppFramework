using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFramework.GeneratorTests;

public class HelpTest(ITestOutputHelper output)
{
    VerifyHelper verifier = new VerifyHelper(output, "CAF");

    [Fact]
    public void Version()
    {
        var version = GetEntryAssemblyVersion();

        verifier.Execute(code: $$"""
ConsoleApp.Run(args, (int x, int y) => { });
""",
    args: "--version",
    expected: $$"""
{{version}}

""");
        // custom
        verifier.Execute(code: $$"""
ConsoleApp.Version = "9999.9999999abcdefg";
ConsoleApp.Run(args, (int x, int y) => { });
""",
   args: "--version",
   expected: """
9999.9999999abcdefg

""");
    }

    [Fact]
    public void VersionOnBuilder()
    {
        var version = GetEntryAssemblyVersion();

        verifier.Execute(code: """
var app = ConsoleApp.Create();
app.Run(args);
""",
    args: "--version",
    expected: $$"""
{{version}}

""");
    }

    [Fact]
    public void Run()
    {
        verifier.Execute(code: """
ConsoleApp.Run(args, (int x, int y) => { });
""",
            args: "--help",
            expected: """
Usage: [options...] [-h|--help] [--version]

Options:
  --x <int>     (Required)
  --y <int>     (Required)

""");
    }

    [Fact]
    public void RunVoid()
    {
        verifier.Execute(code: """
ConsoleApp.Run(args, () => { });
""",
            args: "--help",
            expected: """
Usage: [-h|--help] [--version]

""");
    }

    [Fact]
    public void RootOnly()
    {
        verifier.Execute(code: """
var app = ConsoleApp.Create();
app.Add("", (int x, int y) => { });
app.Run(args);
""",
    args: "--help",
    expected: """
Usage: [options...] [-h|--help] [--version]

Options:
  --x <int>     (Required)
  --y <int>     (Required)

""");
    }

    [Fact]
    public void ListWithoutRoot()
    {
        var code = """
var app = ConsoleApp.Create();
app.Add("a", (int x, int y) => { });
app.Add("ab", (int x, int y) => { });
app.Add("a b c", (int x, int y) => { });
app.Run(args);
""";
        verifier.Execute(code, args: "--help", expected: """
Usage: [command] [-h|--help] [--version]

Commands:
  a
  a b c
  ab

""");
    }

    [Fact]
    public void ListWithRoot()
    {
        var code = """
var app = ConsoleApp.Create();
app.Add("", (int x, int y) => { });
app.Add("a", (int x, int y) => { });
app.Add("ab", (int x, int y) => { });
app.Add("a b c", (int x, int y) => { });
app.Run(args);
""";
        verifier.Execute(code, args: "--help", expected: """
Usage: [command] [options...] [-h|--help] [--version]

Options:
  --x <int>     (Required)
  --y <int>     (Required)

Commands:
  a
  a b c
  ab

""");
    }

    [Fact]
    public void NoArgsOnRootShowsSameHelpTextAsHelpWhenParametersAreRequired()
    {
        var code = """
var app = ConsoleApp.Create();
app.Add("", (int x, int y) => { });
app.Add("a", (int x, int y) => { });
app.Add("ab", (int x, int y) => { });
app.Add("a b c", (int x, int y) => { });
app.Run(args);
""";
        var noArgsOutput = verifier.Error(code, "");
        var helpOutput = verifier.Error(code, "--help");

        noArgsOutput.ShouldBe(helpOutput);
    }

    [Fact]
    public void SelectLeafHelp()
    {
        var code = """
var app = ConsoleApp.Create();
app.Add("", (int x, int y) => { });
app.Add("a", (int x, int y) => { });
app.Add("ab", (int x, int y) => { });
app.Add("a b c", (int x, int y) => { });
app.Run(args);
""";
        verifier.Execute(code, args: "a b c --help", expected: """
Usage: a b c [options...] [-h|--help] [--version]

Options:
  --x <int>     (Required)
  --y <int>     (Required)

""");
    }

    [Fact]
    public void Summary()
    {
        var code = """
var app = ConsoleApp.Create();
app.Add<MyClass>();
app.Run(args);

public class MyClass
{
    /// <summary>
    /// hello my world.
    /// </summary>
    /// <param name="fooBar">-f|-fb, my foo is not bar.</param>
    public void HelloWorld(string fooBar)
    {
        Console.Write("Hello World! " + fooBar);
    }
}
""";
        verifier.Execute(code, args: "--help", expected: """
Usage: [command] [-h|--help] [--version]

Commands:
  hello-world    hello my world.

""");

        verifier.Execute(code, args: "hello-world --help", expected: """
Usage: hello-world [options...] [-h|--help] [--version]

hello my world.

Options:
  -f, -fb, --foo-bar <string>    my foo is not bar. (Required)

""");
    }

    [Fact]
    public void ArgumentOnly()
    {
        verifier.Execute(code: """
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

    [Fact]
    public void ArgumentWithParams()
    {
        verifier.Execute(code: """
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

    [Fact]
    public void Nullable()
    {
        verifier.Execute(code: """
ConsoleApp.Run(args, (int? x = null, string? y = null) => { });
""",
            args: "--help",
            expected: """
Usage: [options...] [-h|--help] [--version]

Options:
  --x <int?>        (Default: null)
  --y <string?>     (Default: null)

""");
    }

    [Fact]
    public void EnumTest()
    {
        verifier.Execute(code: """
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
  --my-fruit <Fruit>        (Default: Apple)
  --more-fruit <Fruit?>     (Default: null)

""");
    }

    [Fact]
    public void Summary2()
    {
        var code = """
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
    public void HelloWorld([Argument]int boo, string fooBar)
    {
        Console.Write("Hello World! " + fooBar);
    }
}
""";
        verifier.Execute(code, args: "hello-world --help", expected: """
Usage: hello-world [arguments...] [options...] [-h|--help] [--version]

hello my world.

Arguments:
  [0] <int>    my boo is not boo.

Options:
  -f, -fb, --foo-bar <string>    my foo, is not bar. (Required)

""");
    }

    [Fact]
    public void HideDefaultValue()
    {
        var code = """
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
        verifier.Execute(code, args: "--help", expected: """
Usage: [options...] [-h|--help] [--version]

Display Hello.

Options:
  -m, --message <string>    Message to show.

""");
    }

    [Fact]
    public void GlobalOptions()
    {
        var code = """
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

        verifier.Execute(code, args: "a --help", expected: """
Usage: a [options...] [-h|--help] [--version]

Options:
  --x <int>             (Required)
  --y <int>             (Required)
  --parameter <int>    param global (Default: 1000)
  --dry-run            run dry dry (Optional)
  --p2, --p3 <int>     param 2 (Required)

""");
    }

    private static string GetEntryAssemblyVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (version == null)
            return "1.0.0";

        // Trim SourceRevisionId (SourceLink feature is enabled by default when using .NET SDK 8 or later)
        var i = version.IndexOf('+');
        if (i != -1)
        {
            version = version.Substring(0, i);
        }

        return version;
    }

    [Fact]
    public void CommandAlias()
    {
        var code = """
var app = ConsoleApp.Create();

app.Add("build|b", () => { Console.Write("build ok"); });
app.Add("test|t", () => { Console.Write("test ok");  });
app.Add<Commands>();

app.Run(args);

public class Commands
{
    /// <summary>Analyze the current package and report errors, but don't build object files.</summary>
    [Command("check|c")]
    public void Check() { Console.Write("check ok"); }

    /// <summary>Build this packages's and its dependencies' documenation.</summary>
    [Command("doc|d")]
    public void Doc() { Console.Write("doc ok"); }
}
""";

        verifier.Execute(code, "--help", """
Usage: [command] [-h|--help] [--version]

Commands:
  build, b
  check, c    Analyze the current package and report errors, but don't build object files.
  doc, d      Build this packages's and its dependencies' documenation.
  test, t

""");
    }
}
