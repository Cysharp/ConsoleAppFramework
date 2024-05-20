using FluentAssertions;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace ConsoleAppFramework.GeneratorTests;

public class Test // (ITestOutputHelper output)
{
    static string[] ToArgs(string args)
    {
        return args.Split(' ');
    }

    [Fact]
    public void SyncRun()
    {
        var result = CSharpGeneratorRunner.CompileAndExecute("""
ConsoleApp.Run(args, (int x, int y) => { Console.Write((x + y)); });
""", ToArgs("--x 10 --y 20"));

        result.Should().Be("30");
    }

    [Fact]
    public void ValidateOne()
    {
        var result = CSharpGeneratorRunner.CompileAndExecute("""
ConsoleApp.Run(args, ([Range(1, 10)]int x, [Range(100, 200)]int y) => { Console.Write((x + y)); });
""", ToArgs("--x 100 --y 140"));

        var expected = """
The field x must be between 1 and 10.


""";

        result.Should().Be(expected);

        Environment.ExitCode.Should().Be(1);
        Environment.ExitCode = 0;
    }

    [Fact]
    public void ValidateTwo()
    {
        var result = CSharpGeneratorRunner.CompileAndExecute("""
ConsoleApp.Run(args, ([Range(1, 10)]int x, [Range(100, 200)]int y) => { Console.Write((x + y)); });
""", ToArgs("--x 100 --y 240"));

        var expected = """
The field x must be between 1 and 10.
The field y must be between 100 and 200.


""";

        result.Should().Be(expected);

        Environment.ExitCode.Should().Be(1);
        Environment.ExitCode = 0;
    }
}