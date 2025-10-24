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
        verifier.Execute("""
var app = ConsoleApp.Create();
var verbose = app.AddGlobalOption<bool>(ref args, "-v");
Console.Write(verbose);
""", "-v", "True");

        verifier.Execute("""
var app = ConsoleApp.Create();
var verbose = app.AddGlobalOption<bool>(ref args, "-no");
Console.Write(verbose);
""", "-v", "False");

        verifier.Execute("""
var app = ConsoleApp.Create();
var verbose = app.AddGlobalOption<bool>(ref args, "-v|--verbose");
Console.Write(verbose);
""", "-v", "True");

        verifier.Execute("""
var app = ConsoleApp.Create();
var verbose = app.AddGlobalOption<bool>(ref args, "-v|--verbose");
Console.Write(verbose);
""", "--verbose", "True");

        verifier.Execute("""
var app = ConsoleApp.Create();
var verbose = app.AddGlobalOption<bool>(ref args, "-v|--verbose|--vo-v");
Console.Write(verbose);
""", "-v", "True");

        verifier.Execute("""
var app = ConsoleApp.Create();
var verbose = app.AddGlobalOption<bool>(ref args, "-v|--verbose|--vo-v");
Console.Write(verbose);
""", "--verbose", "True");

        verifier.Execute("""
var app = ConsoleApp.Create();
var verbose = app.AddGlobalOption<bool>(ref args, "-v|--verbose|--vo-v");
Console.Write(verbose);
""", "--vo-v", "True");
    }

    [Fact]
    public void ArgumentRemove()
    {
        // first
        verifier.Execute("""
var app = ConsoleApp.Create();
var p = app.AddGlobalOption<int>(ref args, "--parameter");
Console.Write(p);
Console.Write("-->" + string.Join(" ", args));
""", "--parameter 100 --x 10 --y 20", "100-->--x 10 --y 20");

        // middle
        verifier.Execute("""
var app = ConsoleApp.Create();
var p = app.AddGlobalOption<int>(ref args, "--parameter");
Console.Write(p);
Console.Write("-->" + string.Join(" ", args));
""", "--x 10 --parameter 100 --y 20", "100-->--x 10 --y 20");

        // last
        verifier.Execute("""
var app = ConsoleApp.Create();
var p = app.AddGlobalOption<int>(ref args, "--parameter");
Console.Write(p);
Console.Write("-->" + string.Join(" ", args));
""", "--x 10 --y 20 --parameter 100", "100-->--x 10 --y 20");
    }

    [Fact]
    public void EnumParse()
    {
        verifier.Execute("""
var app = ConsoleApp.Create();
var p = app.AddGlobalOption<int>(ref args, "--parameter");
var d = app.AddGlobalOption<bool>(ref args, "--dry-run");
var f = app.AddGlobalOption<Fruit>(ref args, "--fruit");
Console.Write(p);
Console.Write(" " + d);
Console.Write(" " + f);
Console.Write("-->" + string.Join(" ", args));

enum Fruit
{
    Orange, Apple, Grape
}

""", "--parameter 100 --x 10 --dry-run --y 20 --fruit grape", "100 True Grape-->--x 10 --y 20");
    }

    [Fact]
    public void DefaultValueForOption()
    {
        verifier.Execute("""
var app = ConsoleApp.Create();
var p = app.AddGlobalOption<int>(ref args, "--parameter", "", -10);
var d = app.AddGlobalOption<bool>(ref args, "--dry-run");
var f = app.AddGlobalOption<Fruit>(ref args, "--fruit", "", Fruit.Apple);
Console.Write(p);
Console.Write(" " + d);
Console.Write(" " + f);
Console.Write("-->" + string.Join(" ", args));

enum Fruit
{
    Orange, Apple, Grape
}

""", "--x 10 --y 20", "-10 False Apple-->--x 10 --y 20");
    }

    [Fact]
    public void RequiredParse()
    {
         verifier.Execute("""
try
{
    var app = ConsoleApp.Create();
    var p = app.AddRequiredGlobalOption<int>(ref args, "--parameter");
}
catch (Exception ex)
{
    Console.Write(ex.Message);
}
""", "--x 10 --dry-run --y 20", "Required argument '--parameter' was not specified.");
    }
}
