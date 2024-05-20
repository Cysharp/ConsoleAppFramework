﻿using Microsoft.CodeAnalysis;

namespace ConsoleAppFramework;

public class WellKnownTypes(Compilation compilation)
{
    INamedTypeSymbol? dateTimeOffset;
    public INamedTypeSymbol DateTimeOffset => dateTimeOffset ??= GetTypeByMetadataName("System.DateTimeOffset");

    INamedTypeSymbol? guid;
    public INamedTypeSymbol Guid => guid ??= GetTypeByMetadataName("System.Guid");

    INamedTypeSymbol? version;
    public INamedTypeSymbol Version => version ??= GetTypeByMetadataName("System.Version");

    INamedTypeSymbol? spanParsable;
    public INamedTypeSymbol? ISpanParsable => spanParsable ??= compilation.GetTypeByMetadataName("System.ISpanParsable`1");

    INamedTypeSymbol? cancellationToken;
    public INamedTypeSymbol CancellationToken => cancellationToken ??= GetTypeByMetadataName("System.Threading.CancellationToken");

    INamedTypeSymbol? task;
    public INamedTypeSymbol Task => task ??= GetTypeByMetadataName("System.Threading.Tasks.Task");

    INamedTypeSymbol? task_T;
    public INamedTypeSymbol Task_T => task_T ??= GetTypeByMetadataName("System.Threading.Tasks.Task`1");

    public bool HasTryParse(ITypeSymbol type)
    {

        if (SymbolEqualityComparer.Default.Equals(type, DateTimeOffset)
         || SymbolEqualityComparer.Default.Equals(type, Guid)
         || SymbolEqualityComparer.Default.Equals(type, Version)
            )
        {
            return true;
        }
        return false;
    }

    INamedTypeSymbol GetTypeByMetadataName(string metadataName)
    {
        var symbol = compilation.GetTypeByMetadataName(metadataName);
        if (symbol == null)
        {
            throw new InvalidOperationException($"Type {metadataName} is not found in compilation.");
        }
        return symbol;
    }
}