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

public class Test(ITestOutputHelper output)
{
    VerifyHelper verifier = new VerifyHelper(output, "CAF");

    [Fact]
    public void SyncRun()
    {
        verifier.Execute("ConsoleApp.Run(args, (int x, int y) => { Console.Write((x + y)); });", "--x 10 --y 20", "30");
    }

    [Fact]
    public void ValidateOne()
    {
        var expected = """
The field x must be between 1 and 10.


""";

        verifier.Execute("""
ConsoleApp.Run(args, ([Range(1, 10)]int x, [Range(100, 200)]int y) => { Console.Write((x + y)); });
""", "--x 100 --y 140", expected);

        Environment.ExitCode.Should().Be(1);
        Environment.ExitCode = 0;
    }

    [Fact]
    public void ValidateTwo()
    {
        var expected = """
The field x must be between 1 and 10.
The field y must be between 100 and 200.


""";

        verifier.Execute("""
ConsoleApp.Run(args, ([Range(1, 10)]int x, [Range(100, 200)]int y) => { Console.Write((x + y)); });
""", "--x 100 --y 240", expected);

        Environment.ExitCode.Should().Be(1);
        Environment.ExitCode = 0;
    }
}