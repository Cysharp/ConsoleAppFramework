using Microsoft.CodeAnalysis;

namespace ConsoleAppFramework.GeneratorTests;


public class DiagnosticsTest
{
    static string Verify(int id, string code)
    {
        var diagnostics = CSharpGeneratorRunner.RunAndGetErrorDiagnostics(code);
        diagnostics.Length.Should().Be(1);
        diagnostics[0].Id.Should().Be("CAF" + id.ToString("000"));
        return GetLocationText(diagnostics[0]);
    }

    static (string, string)[] Verify(string code)
    {
        var diagnostics = CSharpGeneratorRunner.RunAndGetErrorDiagnostics(code);
        return diagnostics.Select(x => (x.Id, GetLocationText(x))).ToArray();
    }

    static string GetLocationText(Diagnostic diagnostic)
    {
        var location = diagnostic.Location;
        var textSpan = location.SourceSpan;
        var sourceTree = location.SourceTree;
        if (sourceTree == null)
        {
            return "";
        }

        var text = sourceTree.GetText().GetSubText(textSpan).ToString();
        return text;
    }

    [Fact]
    public void ArgumentCount()
    {
        var code = """
using System;
using ConsoleAppFramework;

ConsoleApp.Run(args);
""";

        Verify(1, code).Should().Be("ConsoleApp.Run(args)");
    }

}

