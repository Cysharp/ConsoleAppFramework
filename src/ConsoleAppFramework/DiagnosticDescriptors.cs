using Microsoft.CodeAnalysis;

namespace ConsoleAppFramework;

internal sealed class DiagnosticReporter
{
    List<Diagnostic>? diagnostics;

    public bool HasDiagnostics => diagnostics != null && diagnostics.Count != 0;

    public void ReportDiagnostic(DiagnosticDescriptor diagnosticDescriptor, Location location, params object?[]? messageArgs)
    {
        var diagnostic = Diagnostic.Create(diagnosticDescriptor, location, messageArgs);
        if (diagnostics == null)
        {
            diagnostics = new();
        }
        diagnostics.Add(diagnostic);
    }

    public void ReportToContext(SourceProductionContext context)
    {
        if (diagnostics != null)
        {
            foreach (var item in diagnostics)
            {
                context.ReportDiagnostic(item);
            }
        }
    }
}

internal static class DiagnosticDescriptors
{
    const string Category = "GenerateConsoleAppFramework";

    public static void ReportDiagnostic(this SourceProductionContext context, DiagnosticDescriptor diagnosticDescriptor, Location location, params object?[]? messageArgs)
    {
        var diagnostic = Diagnostic.Create(diagnosticDescriptor, location, messageArgs);
        context.ReportDiagnostic(diagnostic);
    }

    public static DiagnosticDescriptor Create(int id, string message)
    {
        return Create(id, message, message);
    }

    public static DiagnosticDescriptor Create(int id, string title, string messageFormat)
    {
        return new DiagnosticDescriptor(
            id: "CAF" + id.ToString("000"),
            title: title,
            messageFormat: messageFormat,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }

    public static DiagnosticDescriptor RequireArgsAndMethod { get; } = Create(
        1,
        "ConsoleApp.Run/RunAsync requires string[] args and lambda/method in arguments.");

    public static DiagnosticDescriptor ReturnTypeLambda { get; } = Create(
        2,
        "Command lambda expressions return type must be void or int or async Task or async Task<int>.",
        "Command lambda expressions return type must be void or int or async Task or async Task<int> but returned '{0}'.");

    public static DiagnosticDescriptor ReturnTypeMethod { get; } = Create(
        3,
        "Command method return type must be void or int or async Task or async Task<int>.",
        "Command method return type must be void or int or async Task or async Task<int> but returned '{0}'.");

    // v5.7.7 supports non-first argument parameters
    //public static DiagnosticDescriptor SequentialArgument { get; } = Create(
    //    4,
    //    "All Argument parameters must be sequential from first.");

    public static DiagnosticDescriptor FunctionPointerCanNotHaveValidation { get; } = Create(
        5,
        "Function pointer can not have validation.");

    public static DiagnosticDescriptor AddCommandMustBeStringLiteral { get; } = Create(
        6,
        "ConsoleAppBuilder.Add string command must be string literal.");

    public static DiagnosticDescriptor DuplicateCommandName { get; } = Create(
        7,
        "Command name is duplicated.",
        "Command name '{0}' is duplicated.");

    public static DiagnosticDescriptor AddInLoopIsNotAllowed { get; } = Create(
        8,
        "ConsoleAppBuilder.Add/UseFilter is not allowed in loop statements(while, do, for, foreach).");

    public static DiagnosticDescriptor CommandHasFilter { get; } = Create(
        9,
        "ConsoleApp.Run does not allow the use of filters, but the function has a filter attribute.");

    public static DiagnosticDescriptor FilterMultipleConstructor { get; } = Create(
        10,
        "ConsoleAppFilter class does not allow multiple constructors.");

    public static DiagnosticDescriptor ClassMultipleConstructor { get; } = Create(
        11,
        "ConsoleAppBuilder.Add<T> class does not allow multiple constructors.");

    public static DiagnosticDescriptor ClassHasNoPublicMethods { get; } = Create(
        12,
        "ConsoleAppBuilder.Add<T> class must have at least one public method.");

    public static DiagnosticDescriptor ClassIsStaticOrAbstract { get; } = Create(
        13,
        "ConsoleAppBuilder.Add<T> class does not allow static or abstract classes.");

    public static DiagnosticDescriptor DefinedInOtherProject { get; } = Create(
        14,
        "ConsoleAppFramework cannot register type/method in another project outside the SourceGenerator referenced project.");

    public static DiagnosticDescriptor DocCommentParameterNameNotMatched { get; } = Create(
        15,
        "Document Comment parameter name '{0}' does not match method parameter name.");

    public static DiagnosticDescriptor ReturnTypeMethodAsyncVoid { get; } = Create(
        16,
        "Command method return type does not allow async void.");

    public static DiagnosticDescriptor DuplicateConfigureGlobalOptions { get; } = Create(
        17,
        "ConfigureGlobalOptions does not allow to invoke twice.");

    public static DiagnosticDescriptor InvalidGlobalOptionsType { get; } = Create(
        18,
        "GlobalOption parameter type only allows compile-time constant(primitives, string, enum) and there nullable.");

    public static DiagnosticDescriptor BindTypeNoValidConstructor { get; } = Create(
        20,
        "Type used with [Bind] must have a parameterless constructor or a primary constructor.",
        "Type '{0}' used with [Bind] must have a parameterless constructor or a primary constructor.");

    public static DiagnosticDescriptor BindUnsupportedPropertyType { get; } = Create(
        21,
        "Property has unsupported type for binding.",
        "Property '{0}' has unsupported type for binding.");

    public static DiagnosticDescriptor BindCircularReference { get; } = Create(
        22,
        "Circular reference detected in type.",
        "Circular reference detected in type '{0}'.");

    public static DiagnosticDescriptor BindConstructorParameterNotMatched { get; } = Create(
        24,
        "Constructor parameter cannot be matched to preceding [Argument] parameters.",
        "Constructor parameter '{0}' in type '{1}' cannot be matched to preceding [Argument] parameters.");

    public static DiagnosticDescriptor BindMultipleConstructors { get; } = Create(
        25,
        "Multiple constructors found; [Bind] requires exactly one public constructor.",
        "Multiple constructors found for type '{0}'; [Bind] requires exactly one public constructor.");

    public static DiagnosticDescriptor GlobalOptionsCannotHaveArguments { get; } = Create(
        26,
        "GlobalOptions cannot have positional arguments",
        "GlobalOptions type '{0}' cannot have positional arguments. Property '{1}' is marked as an argument with [Argument] or 'argument,' comment.");
}
