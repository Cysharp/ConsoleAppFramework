using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Text;

namespace ConsoleAppFramework;

public enum MethodKind
{
    Lambda, Method, FunctionPointer
}

public record class Command
{
    public required bool IsAsync { get; init; } // Task or Task<int>
    public required bool IsVoid { get; init; }  // void or int
    public required bool IsRootCommand { get; init; }
    public required string CommandName { get; init; }
    public required CommandParameter[] Parameters { get; init; }
    public required string Description { get; init; }
    public required MethodKind MethodKind { get; init; }

    public string BuildDelegateSignature(out string? delegateType)
    {
        if (MethodKind == MethodKind.Lambda && Parameters.Any(x => x.HasDefaultValue))
        {
            delegateType = BuildDelegateType("RunCommand");
            return "RunCommand";
        }

        delegateType = null;
        if (MethodKind == MethodKind.FunctionPointer) return BuildFunctionPointerDelegateSignature();

        if (IsAsync)
        {
            if (IsVoid)
            {
                // Func<...,Task>
                if (Parameters.Length == 0)
                {
                    return $"Func<Task>";
                }
                else
                {
                    var parameters = string.Join(", ", Parameters.Select(x => x.Type.ToFullyQualifiedFormatDisplayString()));
                    return $"Func<{parameters}, Task>";
                }
            }
            else
            {
                // Func<...,Task<int>>
                if (Parameters.Length == 0)
                {
                    return $"Func<Task<int>>";
                }
                else
                {
                    var parameters = string.Join(", ", Parameters.Select(x => x.Type.ToFullyQualifiedFormatDisplayString()));
                    return $"Func<{parameters}, Task<int>>";
                }
            }
        }
        else
        {
            if (IsVoid)
            {
                // Action
                if (Parameters.Length == 0)
                {
                    return "Action";
                }
                else
                {
                    var parameters = string.Join(", ", Parameters.Select(x => x.Type.ToFullyQualifiedFormatDisplayString()));
                    return $"Action<{parameters}>";
                }
            }
            else
            {
                // Func
                if (Parameters.Length == 0)
                {
                    return "Func<int>";
                }
                else
                {
                    var parameters = string.Join(", ", Parameters.Select(x => x.Type.ToFullyQualifiedFormatDisplayString()));
                    return $"Func<{parameters}, int>";
                }
            }
        }
    }

    public string BuildFunctionPointerDelegateSignature()
    {
        var retType = (IsAsync, IsVoid) switch
        {
            (true, true) => "Task",
            (true, false) => "Task<int>",
            (false, true) => "void",
            (false, false) => "int"
        };

        var parameters = string.Join(", ", Parameters.Select(x => x.Type.ToFullyQualifiedFormatDisplayString()));
        var comma = Parameters.Length > 0 ? ", " : "";
        return $"delegate* managed<{parameters}{comma}{retType}>";
    }

    public string BuildDelegateType(string delegateName)
    {
        var retType = (IsAsync, IsVoid) switch
        {
            (true, true) => "Task",
            (true, false) => "Task<int>",
            (false, true) => "void",
            (false, false) => "int"
        };

        var parameters = string.Join(", ", Parameters.Select(x => x.ToString()));
        return $"delegate {retType} {delegateName}({parameters});";
    }
}

public record class CommandParameter
{
    public required ITypeSymbol Type { get; init; }
    public required string Name { get; init; }
    public required bool HasDefaultValue { get; init; }
    public object? DefaultValue { get; init; }
    public required ITypeSymbol? CustomParserType { get; init; }
    public required bool IsFromServices { get; init; }
    public required bool IsCancellationToken { get; init; }
    public bool IsParsable => !(IsFromServices || IsCancellationToken);
    public required string[] Aliases { get; init; }
    public required string Description { get; init; }

    public string BuildParseMethod(int argCount, string argumentName, WellKnownTypes wellKnownTypes)
    {
        if (CustomParserType != null)
        {
            return $"if (!{CustomParserType.ToFullyQualifiedFormatDisplayString()}.TryParse(args[++i], out arg{argCount})) ThrowArgumentParseFailed(\"{argumentName}\", args[i]);";
        }

        var tryParseKnownPrimitive = false;
        var tryParseIParsable = false;

        switch (Type.SpecialType)
        {
            case SpecialType.System_String:
                return $"arg{argCount} = args[++i];"; // no parse
            case SpecialType.System_Boolean:
                return $"arg{argCount} = true;"; // bool is true flag
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
                tryParseKnownPrimitive = true;
                break;
            default:
                // Enum
                if (Type.TypeKind == TypeKind.Enum)
                {
                    return $"if (!Enum.TryParse<{Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(args[++i], true, out arg{argCount})) ThrowArgumentParseFailed(\"{argumentName}\", args[i]);";
                }

                // System.DateTimeOffset, System.Guid,  System.Version
                tryParseKnownPrimitive = wellKnownTypes.HasTryParse(Type);

                if (!tryParseKnownPrimitive)
                {
                    // IParsable<T> (BigInteger, Complex, Half, Int128, etc...)
                    var parsable = wellKnownTypes.IParsable;
                    if (parsable != null) // has parsable
                    {
                        tryParseIParsable = Type.AllInterfaces.Any(x => x.EqualsUnconstructedGenericType(parsable));
                    }
                }

                break;
        }

        if (tryParseKnownPrimitive)
        {
            return $"if (!{Type.ToFullyQualifiedFormatDisplayString()}.TryParse(args[++i], out arg{argCount})) ThrowArgumentParseFailed(\"{argumentName}\", args[i]);";
        }
        else if (tryParseIParsable)
        {
            return $"if (!{Type.ToFullyQualifiedFormatDisplayString()}.TryParse(args[++i], null, out arg{argCount})) ThrowArgumentParseFailed(\"{argumentName}\", args[i]);";
        }
        else
        {
            return $"try {{ arg{argCount} = System.Text.Json.JsonSerializer.Deserialize<{Type.ToFullyQualifiedFormatDisplayString()}>(args[++i]); }} catch {{ ThrowArgumentParseFailed(\"{argumentName}\", args[i]); }}";
        }
    }

    public string DefaultValueToString(bool castValue = true)
    {
        if (DefaultValue is bool b)
        {
            return b ? "true" : "false";
        }
        if (DefaultValue is string s)
        {
            return "\"" + s + "\"";
        }
        if (DefaultValue == null)
        {
            if (!castValue) return "null";
            return $"({Type.ToFullyQualifiedFormatDisplayString()})null";
        }

        if (!castValue) return DefaultValue.ToString();
        return $"({Type.ToFullyQualifiedFormatDisplayString()}){DefaultValue}";
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Type.ToFullyQualifiedFormatDisplayString());
        sb.Append(" ");
        sb.Append(Name);
        if (HasDefaultValue)
        {
            sb.Append(" = ");
            sb.Append(DefaultValueToString(castValue: false));
        }

        return sb.ToString();
    }
}