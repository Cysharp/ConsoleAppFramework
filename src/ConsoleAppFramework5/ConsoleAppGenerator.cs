using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConsoleAppFramework;

[Generator(LanguageNames.CSharp)]
public partial class ConsoleAppGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(EmitConsoleAppTemplateSource);

        var source = context.SyntaxProvider.CreateSyntaxProvider((node, ct) =>
        {
            if (node.IsKind(SyntaxKind.InvocationExpression))
            {
                var invocationExpression = (node as InvocationExpressionSyntax);
                if (invocationExpression == null) return false;

                var expr = invocationExpression.Expression as MemberAccessExpressionSyntax;
                if ((expr?.Expression as IdentifierNameSyntax)?.Identifier.Text == "ConsoleApp")
                {
                    var methodName = expr?.Name.Identifier.Text;
                    if (methodName is "Run" or "RunAsync")
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }, (context, ct) => ((InvocationExpressionSyntax)context.Node, context.SemanticModel));

        context.RegisterSourceOutput(source, EmitConsoleAppRun);
    }

    static void EmitConsoleAppTemplateSource(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("ConsoleApp.cs", """
namespace ConsoleAppFramework;

using System;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class ParserAttribute<T> : Attribute
{
}

internal interface IParser<T>
{
    static abstract bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out T result);
}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class OptionAttribute : Attribute
{
    public string[] Aliases { get; }
    public string? Name { get; set; }
    public string? Description { get; set; }

    public OptionAttribute(params string[] aliases)
    {
        this.Aliases = aliases;
    }
}

internal static partial class ConsoleApp
{
    public static void Run(string[] args)
    {
    }

    public static Task RunAsync(string[] args)
    {
        return Task.CompletedTask;
    }

    static void ThrowArgumentParseFailed(string argumentName, string value)
    {
        throw new ArgumentException($"Argument '{argumentName}' parse failed. value: {value}");
    }

    static void ThrowInvalidArgumentName(string name)
    {
        throw new ArgumentException($"Required argument '{name}' does not matched.");
    }

    static void ThrowRequiredArgumentNotParsed(string name)
    {
        throw new ArgumentException($"Require argument '{name}' does not parsed.");
    }

    static System.Collections.Generic.IEnumerable<(int start, int end)> Split(string str)
    {
        var start = 0;
        for (var i = 0; i < str.Length; i++)
        {
            if (str[i] == ',')
            {
                yield return (start, i - 1);
                start = i + 1;
            }
        }
    }
}
""");
    }

    static void EmitConsoleAppRun(SourceProductionContext sourceProductionContext, (InvocationExpressionSyntax, SemanticModel) generatorSyntaxContext)
    {
        var node = generatorSyntaxContext.Item1;
        var model = generatorSyntaxContext.Item2;

        var wellKnownTypes = new WellKnownTypes(model.Compilation);

        var parser = new Parser(sourceProductionContext, node, model);
        var command = parser.ParseAndValidate();
        if (command == null)
        {
            return;
        }

        var emitter = new Emitter(sourceProductionContext, command, wellKnownTypes);
        var code = emitter.Emit();

        sourceProductionContext.AddSource("ConsoleApp.Run.cs", $$"""
namespace ConsoleAppFramework;

using System;
using System.Threading.Tasks;

internal static partial class ConsoleApp
{
{{code}}
}
""");
    }
}