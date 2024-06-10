using Microsoft.CodeAnalysis;

namespace ConsoleAppFramework;

internal static class DiagnosticDescriptors
{
    const string Category = "GenerateConsoleAppFramework";

    public static void ReportDiagnostic(this SourceProductionContext context, DiagnosticDescriptor diagnosticDescriptor, Location location, params object?[]? messageArgs)
    {
#if !TEST
        // must use location.Clone(), incremental cached code + diagnostic craches visual studio however use Clone() can avoid it.
        location = location.Clone();
#endif
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

    public static DiagnosticDescriptor SequentialArgument { get; } = Create(
        4,
        "All Argument parameters must be sequential from first.");

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

    public static DiagnosticDescriptor FilterMultipleConsturtor { get; } = Create(
        10,
        "ConsoleAppFilter class does not allow multiple constructors.");

    public static DiagnosticDescriptor ClassMultipleConsturtor { get; } = Create(
        11,
        "ConsoleAppBuilder.Add<T> class does not allow multiple constructors.");

    public static DiagnosticDescriptor ClassHasNoPublicMethods { get; } = Create(
        12,
        "ConsoleAppBuilder.Add<T> class must have at least one public method.");

    public static DiagnosticDescriptor ClassIsStaticOrAbstract { get; } = Create(
        13,
        "ConsoleAppBuilder.Add<T> class does not allow static or abstract classes.");
}
