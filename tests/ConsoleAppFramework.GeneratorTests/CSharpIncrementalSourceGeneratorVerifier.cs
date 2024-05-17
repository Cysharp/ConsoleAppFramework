using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.CSharp.Testing;

namespace ConsoleAppFramework.GeneratorTests;

// https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md#unit-testing-of-generators

public static class CSharpIncrementalSourceGeneratorVerifier<TSourceGenerator>
    where TSourceGenerator : IIncrementalGenerator, new()
{
    public class Test : SourceGeneratorTest<XUnitVerifier>
    {
        public CSharpCompilationOptions CompilationOptions { get; set; } = new(OutputKind.DynamicallyLinkedLibrary);
        protected override CompilationOptions CreateCompilationOptions() => CompilationOptions;
        public CSharpParseOptions ParseOptions { get; set; } = new(languageVersion: LanguageVersion.CSharp12, kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.Parse);
        protected override ParseOptions CreateParseOptions() => ParseOptions;
        public AnalyzerConfigOptionsProvider? AnalyzerConfigOptionsProvider { get; set; }

        protected override string DefaultFileExt => "cs";
        public override string Language => LanguageNames.CSharp;
        protected override IEnumerable<ISourceGenerator> GetSourceGenerators() => new[] { new TSourceGenerator().AsSourceGenerator() };
        protected override GeneratorDriver CreateGeneratorDriver(Project project, ImmutableArray<ISourceGenerator> sourceGenerators)
            => CSharpGeneratorDriver.Create(
                sourceGenerators,
                project.AnalyzerOptions.AdditionalFiles,
                (CSharpParseOptions)project.ParseOptions!,
                AnalyzerConfigOptionsProvider ?? project.AnalyzerOptions.AnalyzerConfigOptionsProvider);
    }
}
