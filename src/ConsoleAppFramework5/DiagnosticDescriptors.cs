using Microsoft.CodeAnalysis;

namespace ConsoleAppFramework;

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
        "Run lambda expressions return type must be void or int or async Task or async Task<int>.",
        "Run lambda expressions return type must be void or int or async Task or async Task<int> but returned '{0}'.");

    public static DiagnosticDescriptor ReturnTypeMethod { get; } = Create(
        3,
        "Run referenced method return type must be void or int or async Task or async Task<int>.",
        "Run referenced method return type must be void or int or async Task or async Task<int> but returned '{0}'.");

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
}
