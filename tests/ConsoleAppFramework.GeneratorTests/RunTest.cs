using FluentAssertions;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace ConsoleAppFramework.GeneratorTests;

public class Test // (ITestOutputHelper output)
{
    static void Verify(int id, string code, bool allowMultipleError = false)
    {
        var diagnostics = CSharpGeneratorRunner.RunAndGetErrorDiagnostics(code);
        if (!allowMultipleError)
        {
            diagnostics.Length.Should().Be(1);
            diagnostics[0].Id.Should().Be("CAF" + id.ToString("000"));
        }
        else
        {
            diagnostics.Select(x => x.Id).Should().Contain("CAF" + id.ToString("000"));
        }
    }

    static string[] ToArgs(string args)
    {
        return args.Split(' ');
    }

    [Fact]
    public void SyncRun()
    {
        var result = CSharpGeneratorRunner.CompileAndExecute("""
using System;
using ConsoleAppFramework;

ConsoleApp.Run(args, (int x, int y) => { Console.WriteLine((x + y)); });
""", ToArgs("--x 10 --y 20"));

        result.Should().Be("""
30

""");
    }
}