using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFramework.GeneratorTests;

public class ConsoleAppBuilderTest
{
    static string[] ToArgs(string args) => args.Split(' ');

    [Fact]
    public void BuilderRun()
    {
        var code = """
var builder = ConsoleApp.CreateBuilder();
builder.Add("foo", (int x, int y) => { Console.Write(x + y); });
builder.Add("bar", (int x, int y = 10) => { Console.Write(x + y); });
builder.Add("baz", int (int x, string y) => { Console.Write(x + y); return 10; });
builder.Add("boz", async Task (int x) => { await Task.Yield(); Console.Write(x * 2); });
builder.Run(args);
""";

        CSharpGeneratorRunner.CompileAndExecute(code, ToArgs("foo --x 10 --y 20")).Should().Be("30");
        CSharpGeneratorRunner.CompileAndExecute(code, ToArgs("bar --x 20 --y 30")).Should().Be("50");
        CSharpGeneratorRunner.CompileAndExecute(code, ToArgs("bar --x 20")).Should().Be("30");
        Environment.ExitCode.Should().Be(0);
        CSharpGeneratorRunner.CompileAndExecute(code, ToArgs("baz --x 40 --y takoyaki")).Should().Be("40takoyaki");
        Environment.ExitCode.Should().Be(10);
        Environment.ExitCode = 0;

        CSharpGeneratorRunner.CompileAndExecute(code, ToArgs("boz --x 40")).Should().Be("80");
    }

    [Fact]
    public void BuilderRunAsync()
    {
        var code = """
var builder = ConsoleApp.CreateBuilder();
builder.Add("foo", (int x, int y) => { Console.Write(x + y); });
builder.Add("bar", (int x, int y = 10) => { Console.Write(x + y); });
builder.Add("baz", int (int x, string y) => { Console.Write(x + y); return 10; });
builder.Add("boz", async Task (int x) => { await Task.Yield(); Console.Write(x * 2); });
await builder.RunAsync(args);
""";

        CSharpGeneratorRunner.CompileAndExecute(code, ToArgs("foo --x 10 --y 20")).Should().Be("30");
        CSharpGeneratorRunner.CompileAndExecute(code, ToArgs("bar --x 20 --y 30")).Should().Be("50");
        CSharpGeneratorRunner.CompileAndExecute(code, ToArgs("bar --x 20")).Should().Be("30");
        Environment.ExitCode.Should().Be(0);
        CSharpGeneratorRunner.CompileAndExecute(code, ToArgs("baz --x 40 --y takoyaki")).Should().Be("40takoyaki");
        Environment.ExitCode.Should().Be(10);
        Environment.ExitCode = 0;

        CSharpGeneratorRunner.CompileAndExecute(code, ToArgs("boz --x 40")).Should().Be("80");
    }

    [Fact]
    public void AddClass()
    {
        var code = """
var builder = ConsoleApp.CreateBuilder();
builder.Add<MyClass>();
await builder.RunAsync(args);

public class MyClass
{
    public void Do()
    {
        Console.Write("yeah");
    }

    public void Sum(int x, int y)
    {
        Console.Write(x + y);
    }

    public void Echo(string msg)
    {
        Console.Write(msg);
    }

    void Echo()
    {
    }

    public static void Sum()
    {
    }
}
""";

        CSharpGeneratorRunner.CompileAndExecute(code, ToArgs("do")).Should().Be("yeah");

    }
}


