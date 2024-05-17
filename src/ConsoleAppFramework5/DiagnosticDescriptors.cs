using Microsoft.CodeAnalysis;

namespace ConsoleAppFramework;

internal static class DiagnosticDescriptors
{
    const string Category = "GenerateConsoleAppFramework";

    public static readonly DiagnosticDescriptor RequireArgsAndMethod = new(
        id: "CAF001",
        title: "ConsoleApp methods require string[] args and lambda/method.",
        messageFormat: "ConsoleApp.Run/RunAsync requires string[] args and lambda/method in arguments.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static Diagnostic Create(this DiagnosticDescriptor diagnostic, Location location, params object?[]? messageArgs)
    {
        return Diagnostic.Create(diagnostic, location, messageArgs);
    }
}
