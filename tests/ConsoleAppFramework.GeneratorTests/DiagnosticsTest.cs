using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Text;

namespace ConsoleAppFramework.GeneratorTests;

using VerifyCS = CSharpIncrementalSourceGeneratorVerifier<ConsoleAppGenerator>;


public class DiagnosticsTest
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

    [Fact]
    public async Task Foo()
    {
        var code = """
using System;
using ConsoleAppFramework;
using System.ComponentModel.DataAnnotations;

ConsoleApp.Run(args, ([Range(1, 10)]int x, [Range(100, 200)]int y) => { Console.Write((x + y)); });
""";
        var generated = "expected generated code";
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { code },
                GeneratedSources =
                {
                    (typeof(ConsoleAppGenerator), "GeneratedFileName", SourceText.From(generated, Encoding.UTF8, SourceHashAlgorithm.Sha256)),
                },
            },

        }.RunAsync();
    }

}

