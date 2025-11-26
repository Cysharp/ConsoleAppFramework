using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFramework.GeneratorTests;

public class GlobalOptionTest
{
    VerifyHelper verifier = new VerifyHelper("CAF");

    [Test]
    public async Task BooleanParseCheck()
    {
        string BuildCode(string parameter)
        {
            return $$"""
var app = ConsoleApp.Create();
app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) => builder.AddGlobalOption<bool>("{{parameter}}", ""));
app.Add("", (ConsoleAppContext context) => Console.Write(context.GlobalOptions));
app.Run(args);
""";
        }

        await verifier.Execute(BuildCode("-v"), "-v", "True");
        await verifier.Execute(BuildCode("-no"), "-v", "False");
        await verifier.Execute(BuildCode("-v|--verbose"), "-v", "True");
        await verifier.Execute(BuildCode("-v|--verbose"), "--verbose", "True");
        await verifier.Execute(BuildCode("-v|--verbose|--vo-v"), "-v", "True");
        await verifier.Execute(BuildCode("-v|--verbose|--vo-v"), "--verbose", "True");
        await verifier.Execute(BuildCode("-v|--verbose|--vo-v"), "--vo-v", "True");
        await verifier.Execute(BuildCode("-v|--verbose|--vo-v"), "--no", "False");
    }

    [Test]
    public async Task ArgumentRemove()
    {
        var code = $$"""
var app = ConsoleApp.Create();
app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    var p = builder.AddGlobalOption("--parameter", "", 0);
    return p;
});
app.Add("", (int x, int y, ConsoleAppContext context) =>
{
    Console.Write($"{context.GlobalOptions} -> ({x}, {y})");
});
app.Run(args);
""";

        // first
        await verifier.Execute(code, "--parameter 100 --x 10 --y 20", "100 -> (10, 20)");

        // middle
        await verifier.Execute(code, "--x 10 --parameter 100 --y 20", "100 -> (10, 20)");

        // last
        await verifier.Execute(code, "--x 10 --y 20 --parameter 100", "100 -> (10, 20)");
    }

    [Test]
    public async Task EnumParse()
    {
        await verifier.Execute("""
var app = ConsoleApp.Create();
app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    var p = builder.AddGlobalOption("--parameter", "", 0);
    var d = builder.AddGlobalOption<bool>("--dry-run", "");
    var f = builder.AddGlobalOption("--fruit", "", Fruit.Orange);
    return (p, d, f);
});
app.Add("", (int x, int y, ConsoleAppContext context) =>
{
    Console.Write($"{context.GlobalOptions} -> {(x, y)}");
});
app.Run(args);

enum Fruit
{
    Orange, Apple, Grape
}
""", "--parameter 100 --x 10 --dry-run --y 20 --fruit grape", "(100, True, Grape) -> (10, 20)");
    }

    [Test]
    public async Task EnumErrorShowsValidValues()
    {
        var result = verifier.Error("""
ConsoleApp.Log = x => Console.Write(x);

var app = ConsoleApp.Create();
app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    var fruit = builder.AddGlobalOption("--fruit", "", Fruit.Apple);
    return fruit;
});

app.Add("", (ConsoleAppContext context) => { });
app.Run(args);

enum Fruit
{
    Orange, Apple, Grape
}
""", "--fruit Potato");

        await Assert.That(result.Stdout).Contains("Argument '--fruit' is invalid. Provided value: Potato. Valid values: Orange, Apple, Grape");
        await Assert.That(result.ExitCode).IsEqualTo(1);
    }

    [Test]
    public async Task DefaultValueForOption()
    {
        await verifier.Execute("""
var app = ConsoleApp.Create();
app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    var p = builder.AddGlobalOption<int>("--parameter", "", -10);
    var d = builder.AddGlobalOption<bool>("--dry-run", "");
    var f = builder.AddGlobalOption("--fruit", "", Fruit.Apple);
    return (p, d, f);
});
app.Add("", (int x, int y, ConsoleAppContext context) =>
{
    Console.Write($"{context.GlobalOptions} -> {(x, y)}");
});
app.Run(args);

enum Fruit
{
    Orange, Apple, Grape
}

""", "--x 10 --y 20", "(-10, False, Apple) -> (10, 20)");
    }

    [Test]
    public async Task RequiredParse()
    {
        var error = verifier.Error("""
var app = ConsoleApp.Create();
app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    var p = builder.AddRequiredGlobalOption<int>("--parameter", "");
    var d = builder.AddGlobalOption<bool>("--dry-run", "");
    return (p, d);
});
app.Add("", (int x, int y, ConsoleAppContext context) =>
{
    Console.Write($"{context.GlobalOptions} -> {(x, y)}");
});
app.Run(args);
""", "--x 10 --dry-run --y 20");

        error.Stdout.Contains("Required argument '--parameter' was not specified.");
    }

    [Test]
    public async Task NamedParameter()
    {
        await verifier.Execute("""
var app = ConsoleApp.Create();
app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    var p = builder.AddGlobalOption<int>("--parameter", defaultValue: 1000);
    var d = builder.AddGlobalOption<bool>(description: "foo", name: "--dry-run");
    return (p, d);
});
app.Add("", (int x, int y, ConsoleAppContext context) =>
{
    Console.Write($"{context.GlobalOptions} -> {(x, y)}");
});
app.Run(args);
""", "--x 10 --dry-run --y 20", "(1000, True) -> (10, 20)");
    }

    [Test]
    public async Task DoubleDashEscape()
    {
        await verifier.Execute("""
var app = ConsoleApp.Create();

app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    var flag = builder.AddGlobalOption<string>("--global-flag");
    return new GlobalOptions(flag);
});

app.UseFilter<SomeFilter>();
app.Add<Commands>();
app.Run(args);

internal record GlobalOptions(string Flag);

internal class Commands
{
    [Command("some-command")]
    public async Task SomeCommand([Argument] string commandArg, ConsoleAppContext context)
    {
        Console.WriteLine($"ARG: {commandArg}");
        Console.WriteLine($"ESCAPED: {string.Join(", ", context.EscapedArguments.ToArray()!)}");
    }
}

internal class SomeFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"FLAG: {((GlobalOptions)context.GlobalOptions!).Flag}");
        await Next.InvokeAsync(context, cancellationToken);
    }
}
""", "some-command hello --global-flag flag-value -- more args here", """
FLAG: flag-value
ARG: hello
ESCAPED: more, args, here

""");
    }
}
