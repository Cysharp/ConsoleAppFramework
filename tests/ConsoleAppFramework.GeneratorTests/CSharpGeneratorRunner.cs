using ConsoleAppFramework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Xunit.Abstractions;

public static class CSharpGeneratorRunner
{
    static Compilation baseCompilation = default!;

    [ModuleInitializer]
    public static void InitializeCompilation()
    {
        // running .NET Core system assemblies dir path
        var baseAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var systemAssemblies = Directory.GetFiles(baseAssemblyPath)
            .Where(x =>
            {
                var fileName = Path.GetFileName(x);
                if (fileName.EndsWith("Native.dll")) return false;
                return fileName.StartsWith("System") || (fileName is "mscorlib.dll" or "netstandard.dll");
            });

        var references = systemAssemblies
            .Select(x => MetadataReference.CreateFromFile(x))
            .ToArray();

        var globalUsings = """
global using System;
global using System.Threading.Tasks;
global using System.ComponentModel.DataAnnotations;
global using ConsoleAppFramework;
""";

        var compilation = CSharpCompilation.Create("generatortest",
            references: references,
            syntaxTrees: [CSharpSyntaxTree.ParseText(globalUsings)],
            options: new CSharpCompilationOptions(OutputKind.ConsoleApplication)); // .exe

        baseCompilation = compilation;
    }

    public static (Compilation, ImmutableArray<Diagnostic>) RunGenerator(string source, string[]? preprocessorSymbols = null, AnalyzerConfigOptionsProvider? options = null)
    {
        if (preprocessorSymbols == null)
        {
            preprocessorSymbols = new[] { "NET8_0_OR_GREATER" };
        }
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp12, preprocessorSymbols: preprocessorSymbols); // 12

        var driver = CSharpGeneratorDriver.Create(new ConsoleAppGenerator()).WithUpdatedParseOptions(parseOptions);
        if (options != null)
        {
            driver = (Microsoft.CodeAnalysis.CSharp.CSharpGeneratorDriver)driver.WithUpdatedAnalyzerConfigOptions(options);
        }

        var compilation = baseCompilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(source, parseOptions));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);
        return (newCompilation, diagnostics);
    }

    public static string CompileAndExecute(string source, string[] args, string[]? preprocessorSymbols = null, AnalyzerConfigOptionsProvider? options = null)
    {
        var (compilation, diagnostics) = RunGenerator(source, preprocessorSymbols, options);

        using var ms = new MemoryStream();
        var emitResult = compilation.Emit(ms);
        if (!emitResult.Success)
        {
            throw new InvalidOperationException("Emit Failed\r\n" + string.Join("\r\n", emitResult.Diagnostics.Select(x => x.ToString())));
        }

        ms.Position = 0;

        // capture stdout log
        // modify global stdout so can't run in parallel unit-test
        var originalOut = Console.Out;
        try
        {
            var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);

            // load and invoke Main(args)
            var loadContext = new AssemblyLoadContext("source-generator", isCollectible: true);
            var assembly = loadContext.LoadFromStream(ms);
            assembly.EntryPoint!.Invoke(null, new object[] { args });
            loadContext.Unload();

            return stringWriter.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}

public class VerifyHelper(ITestOutputHelper output, string idPrefix)
{
    public void Ok(string code, [CallerArgumentExpression("code")] string? codeExpr = null)
    {
        output.WriteLine(codeExpr);

        var (compilation, diagnostics) = CSharpGeneratorRunner.RunGenerator(code);
        foreach (var item in diagnostics)
        {
            output.WriteLine(item.ToString());
        }
        OutputGeneratedCode(compilation);

        diagnostics.Length.Should().Be(0);
    }

    public void Verify(int id, string code, string diagnosticsCodeSpan, [CallerArgumentExpression("code")] string? codeExpr = null)
    {
        output.WriteLine(codeExpr);

        var (compilation, diagnostics) = CSharpGeneratorRunner.RunGenerator(code);
        foreach (var item in diagnostics)
        {
            output.WriteLine(item.ToString());
        }
        OutputGeneratedCode(compilation);

        diagnostics.Length.Should().Be(1);
        diagnostics[0].Id.Should().Be(idPrefix + id.ToString("000"));
        var text = GetLocationText(diagnostics[0]);
        text.Should().Be(diagnosticsCodeSpan);
    }

    public (string, string)[] Verify(string code, [CallerArgumentExpression("code")] string? codeExpr = null)
    {
        output.WriteLine(codeExpr);

        var (compilation, diagnostics) = CSharpGeneratorRunner.RunGenerator(code);
        OutputGeneratedCode(compilation);
        return diagnostics.Select(x => (x.Id, GetLocationText(x))).ToArray();
    }

    string GetLocationText(Diagnostic diagnostic)
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

    void OutputGeneratedCode(Compilation compilation)
    {
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            // only shows ConsoleApp.Run generated code
            if (!syntaxTree.FilePath.Contains("ConsoleApp.Run.cs")) continue;
            output.WriteLine(syntaxTree.ToString());
        }
    }
}