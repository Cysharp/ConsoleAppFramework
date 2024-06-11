using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace ConsoleAppFramework;

public class EquatableTypeSymbol(ITypeSymbol typeSymbol) : IEquatable<EquatableTypeSymbol>
{
    // check this two types usage for Equality
    public ITypeSymbol TypeSymbol => typeSymbol;
    public ImmutableArray<ISymbol> GetMembers() => typeSymbol.GetMembers();

    public TypeKind TypeKind { get; } = typeSymbol.TypeKind;
    public SpecialType SpecialType { get; } = typeSymbol.SpecialType;


    public string ToFullyQualifiedFormatDisplayString() => typeSymbol.ToFullyQualifiedFormatDisplayString();
    public string ToDisplayString(NullableFlowState state, SymbolDisplayFormat format) => typeSymbol.ToDisplayString(state, format);

    public bool Equals(EquatableTypeSymbol other)
    {
        // TODO:
        return false;
    }
}
