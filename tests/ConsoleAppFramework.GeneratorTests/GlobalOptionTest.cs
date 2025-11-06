using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFramework.GeneratorTests;

public class GlobalOptionTest(ITestOutputHelper output)
{
    VerifyHelper verifier = new VerifyHelper(output, "CAF");

    [Fact]
    public void BooleanParseCheck()
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

        verifier.Execute(BuildCode("-v"), "-v", "True");
        verifier.Execute(BuildCode("-no"), "-v", "False");
        verifier.Execute(BuildCode("-v|--verbose"), "-v", "True");
        verifier.Execute(BuildCode("-v|--verbose"), "--verbose", "True");
        verifier.Execute(BuildCode("-v|--verbose|--vo-v"), "-v", "True");
        verifier.Execute(BuildCode("-v|--verbose|--vo-v"), "--verbose", "True");
        verifier.Execute(BuildCode("-v|--verbose|--vo-v"), "--vo-v", "True");
        verifier.Execute(BuildCode("-v|--verbose|--vo-v"), "--no", "False");
    }

    [Fact]
    public void ArgumentRemove()
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
        verifier.Execute(code, "--parameter 100 --x 10 --y 20", "100 -> (10, 20)");

        // middle
        verifier.Execute(code, "--x 10 --parameter 100 --y 20", "100 -> (10, 20)");

        // last
        verifier.Execute(code, "--x 10 --y 20 --parameter 100", "100 -> (10, 20)");
    }

    [Fact]
    public void EnumParse()
    {
        verifier.Execute("""
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

    [Fact]
    public void DefaultValueForOption()
    {
        verifier.Execute("""
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

    [Fact]
    public void RequiredParse()
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

        error.Contains("Required argument '--parameter' was not specified.");
    }

    [Fact]
    public void NamedParameter()
    {
        verifier.Execute("""
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
}
