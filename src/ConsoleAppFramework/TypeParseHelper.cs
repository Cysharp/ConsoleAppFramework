using Microsoft.CodeAnalysis;

namespace ConsoleAppFramework;

/// <summary>
/// Classifies how a type should be parsed from command-line arguments.
/// </summary>
public enum ParseCategory
{
    /// <summary>String type - direct assignment, no parsing.</summary>
    String,
    /// <summary>Boolean type - flag (presence = true) or explicit "true"/"false".</summary>
    Boolean,
    /// <summary>Primitive with simple TryParse(string, out T).</summary>
    Primitive,
    /// <summary>Enum type - uses Enum.TryParse&lt;T&gt;.</summary>
    Enum,
    /// <summary>Array of ISpanParsable elements - uses TrySplitParse.</summary>
    Array,
    /// <summary>ISpanParsable&lt;T&gt; - needs TryParse(string, IFormatProvider?, out T).</summary>
    SpanParsable,
    /// <summary>Fallback to JSON deserialization.</summary>
    Json
}

/// <summary>
/// Information about how to parse a type.
/// </summary>
public readonly record struct ParseInfo(
    ParseCategory Category,
    string FullTypeName,
    string? ArrayElementTypeName = null
) : IEquatable<ParseInfo>
{
    public bool Equals(ParseInfo other) =>
        Category == other.Category &&
        FullTypeName == other.FullTypeName &&
        ArrayElementTypeName == other.ArrayElementTypeName;

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + Category.GetHashCode();
            hash = hash * 31 + (FullTypeName?.GetHashCode() ?? 0);
            hash = hash * 31 + (ArrayElementTypeName?.GetHashCode() ?? 0);
            return hash;
        }
    }
}

/// <summary>
/// Shared utility for analyzing types and generating parse expressions.
/// </summary>
internal static class TypeParseHelper
{
    /// <summary>
    /// Analyzes a type and returns how it should be parsed.
    /// </summary>
    public static ParseInfo AnalyzeType(ITypeSymbol type, WellKnownTypes wellKnownTypes)
    {
        // Unwrap Nullable<T>
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            type = ((INamedTypeSymbol)type).TypeArguments[0];
        }

        var fullTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Check SpecialType first
        switch (type.SpecialType)
        {
            case SpecialType.System_String:
                return new ParseInfo(ParseCategory.String, fullTypeName);

            case SpecialType.System_Boolean:
                return new ParseInfo(ParseCategory.Boolean, fullTypeName);

            case SpecialType.System_Char:
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Decimal:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_DateTime:
                return new ParseInfo(ParseCategory.Primitive, fullTypeName);
        }

        // Enum
        if (type.TypeKind == TypeKind.Enum)
        {
            return new ParseInfo(ParseCategory.Enum, fullTypeName);
        }

        // Array
        if (type.TypeKind == TypeKind.Array)
        {
            var elementType = ((IArrayTypeSymbol)type).ElementType;
            if (ImplementsISpanParsable(elementType, wellKnownTypes))
            {
                var elementTypeName = elementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                return new ParseInfo(ParseCategory.Array, fullTypeName, elementTypeName);
            }
            // Array of non-ISpanParsable elements falls back to JSON
            return new ParseInfo(ParseCategory.Json, fullTypeName);
        }

        // Known types with simple TryParse (Guid, DateTimeOffset, TimeSpan, etc.)
        if (wellKnownTypes.HasTryParse(type))
        {
            return new ParseInfo(ParseCategory.Primitive, fullTypeName);
        }

        // ISpanParsable<T> (BigInteger, Complex, Half, Int128, etc.)
        if (ImplementsISpanParsable(type, wellKnownTypes))
        {
            return new ParseInfo(ParseCategory.SpanParsable, fullTypeName);
        }

        // Fallback to JSON
        return new ParseInfo(ParseCategory.Json, fullTypeName);
    }

    /// <summary>
    /// Builds the TryParse expression for a type (without index handling or error handling).
    /// </summary>
    /// <returns>The parse expression, e.g., "int.TryParse(input, out var result)"</returns>
    public static string BuildParseExpression(ParseInfo info, string inputExpr, string outVar)
    {
        return info.Category switch
        {
            ParseCategory.Primitive => $"{info.FullTypeName}.TryParse({inputExpr}, {outVar})",
            ParseCategory.Enum => $"Enum.TryParse<{info.FullTypeName}>({inputExpr}, true, {outVar})",
            ParseCategory.SpanParsable => $"{info.FullTypeName}.TryParse({inputExpr}, null, {outVar})",
            ParseCategory.Array => $"TrySplitParse({inputExpr}, {outVar})",
            _ => throw new InvalidOperationException($"Cannot build parse expression for {info.Category}")
        };
    }

    /// <summary>
    /// Builds the error throw expression for parse failures.
    /// </summary>
    public static string BuildThrowExpression(ParseInfo info, string argName, string inputExpr)
    {
        return info.Category == ParseCategory.Enum
            ? $"ThrowArgumentParseFailedEnum(typeof({info.FullTypeName}), \"{argName}\", {inputExpr})"
            : $"ThrowArgumentParseFailed(\"{argName}\", {inputExpr})";
    }

    static bool ImplementsISpanParsable(ITypeSymbol type, WellKnownTypes wellKnownTypes)
    {
        var parsable = wellKnownTypes.ISpanParsable;
        return parsable != null && type.AllInterfaces.Any(x => x.EqualsUnconstructedGenericType(parsable));
    }
}
