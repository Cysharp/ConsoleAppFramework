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

    public static readonly DiagnosticDescriptor RequireArgsAndMethod = Create(
        1,
        "ConsoleApp.Run/RunAsync requires string[] args and lambda/method in arguments.");

    public static readonly DiagnosticDescriptor ReturnTypeLambda = Create(
        2,
        "Run lambda expressions return type must be void or int or async Task or async Task<int>.",
        "Run lambda expressions return type must be void or int or async Task or async Task<int> but returned '{0}'.");

    public static readonly DiagnosticDescriptor ReturnTypeMethod = Create(
       3,
       "Run referenced method return type must be void or int or async Task or async Task<int>.",
       "Run referenced method return type must be void or int or async Task or async Task<int> but returned '{0}'.");

    public static readonly DiagnosticDescriptor SequentialArgument = Create(
       4,
       "All Argument parameters must be sequential from first.");

    public static readonly DiagnosticDescriptor FunctionPointerCanNotHaveValidation = Create(
       5,
       "Function pointer can not have validation.");

    public static readonly DiagnosticDescriptor RequireCommandAndMethod = Create(
        6,
        "ConsoleAppBuilder.Add requires string command and lambda/method in arguments or use Add<T>.");

    public static readonly DiagnosticDescriptor AddCommandMustBeStringLiteral = Create(
        6,
        "ConsoleAppBuilder.Add string command must be string literal.");
}
