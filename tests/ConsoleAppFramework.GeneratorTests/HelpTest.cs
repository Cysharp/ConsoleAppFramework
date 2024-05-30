using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace ConsoleAppFramework.GeneratorTests;

public class HelpTest(ITestOutputHelper output)
{
    VerifyHelper verifier = new VerifyHelper(output, "CAF");

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
app.Add("a/b/c", (int x, int y) => { });
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
app.Add("a/b/c", (int x, int y) => { });
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
    public void SelectLeafHelp()
    {
        var code = """
var app = ConsoleApp.Create();
app.Add("", (int x, int y) => { });
app.Add("a", (int x, int y) => { });
app.Add("ab", (int x, int y) => { });
app.Add("a/b/c", (int x, int y) => { });
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
  -f|-fb|--foo-bar <string>    my foo is not bar. (Required)

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
    /// <param name="fooBar">-f|-fb, my foo is not bar.</param>
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
  -f|-fb|--foo-bar <string>    my foo is not bar. (Required)

""");
    }
}
