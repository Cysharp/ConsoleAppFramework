using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace ConsoleAppFramework;

public class EquatableTypeSymbol(ITypeSymbol typeSymbol) : IEquatable<EquatableTypeSymbol>
{
    // Used for build argument parser, maybe ok to equals name.
    public ITypeSymbol TypeSymbol => typeSymbol;

    // GetMembers is called for Enum and fields is not condition for command equality.
    public ImmutableArray<ISymbol> GetMembers() => typeSymbol.GetMembers();

    public TypeKind TypeKind { get; } = typeSymbol.TypeKind;
    public SpecialType SpecialType { get; } = typeSymbol.SpecialType;

    public string ToFullyQualifiedFormatDisplayString() => typeSymbol.ToFullyQualifiedFormatDisplayString();
    public string ToDisplayString(NullableFlowState state, SymbolDisplayFormat format) => typeSymbol.ToDisplayString(state, format);

    public bool Equals(EquatableTypeSymbol other)
    {
        if (this.TypeKind != other.TypeKind) return false;
        if (this.SpecialType != other.SpecialType) return false;
        if (this.TypeSymbol.Name != other.TypeSymbol.Name) return false;

        return this.TypeSymbol.EqualsNamespaceAndName(other.TypeSymbol);
    }
}

// for filter
public class EquatableTypeSymbolWithKeyedServiceKey
    : EquatableTypeSymbol, IEquatable<EquatableTypeSymbolWithKeyedServiceKey>
{
    public bool IsKeyedService { get; }
    public string? FormattedKeyedServiceKey { get; }

    public EquatableTypeSymbolWithKeyedServiceKey(IParameterSymbol symbol)
        : base(symbol.Type)
    {
        var keyedServciesAttr = symbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "FromKeyedServicesAttribute");
        if (keyedServciesAttr != null)
        {
            this.IsKeyedService = true;
            this.FormattedKeyedServiceKey = CommandParameter.GetFormattedKeyedServiceKey(keyedServciesAttr.ConstructorArguments[0].Value);
        }
    }

    public bool Equals(EquatableTypeSymbolWithKeyedServiceKey other)
    {
        if (base.Equals(other))
        {
            if (IsKeyedService != other.IsKeyedService) return false;
            if (FormattedKeyedServiceKey != other.FormattedKeyedServiceKey) return false;
            return true;
        }

        return false;
    }
}

static class EquatableTypeSymbolExtensions
{
    public static EquatableTypeSymbol ToEquatable(this ITypeSymbol typeSymbol) => new(typeSymbol);
}
