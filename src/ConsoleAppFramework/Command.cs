using Microsoft.CodeAnalysis;
using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

namespace ConsoleAppFramework;

public enum MethodKind
{
    Lambda, Method, FunctionPointer
}

public enum DelegateBuildType
{
    MakeDelegateWhenHasDefaultValue,
    OnlyActionFunc,
    None
}

public record class Command
{
    public required bool IsAsync { get; init; } // Task or Task<int>
    public required bool IsVoid { get; init; }  // void or int
    
    public bool IsRootCommand => Name == "";
    public required string Name { get; init; }

    public required CommandParameter[] Parameters { get; init; }
    public required string Description { get; init; }
    public required MethodKind MethodKind { get; init; }
    public required DelegateBuildType DelegateBuildType { get; init; }
    public CommandMethodInfo? CommandMethodInfo { get; set; } // can set...!
    public required FilterInfo[] Filters { get; init; }
    public bool HasFilter => Filters.Length != 0;

    public string? BuildDelegateSignature(out string? delegateType)
    {
        if (DelegateBuildType == DelegateBuildType.MakeDelegateWhenHasDefaultValue)
        {
            if (MethodKind == MethodKind.Lambda && Parameters.Any(x => x.HasDefaultValue || x.IsParams))
            {
                delegateType = BuildDelegateType("RunCommand");
                return "RunCommand";
            }
        }

        delegateType = null;

        if (DelegateBuildType == DelegateBuildType.None)
        {
            return null;
        }

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
                    var parameters = string.Join(", ", Parameters.Select(x => x.ToTypeDisplayString()));
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
                    var parameters = string.Join(", ", Parameters.Select(x => x.ToTypeDisplayString()));
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
                    var parameters = string.Join(", ", Parameters.Select(x => x.ToTypeDisplayString()));
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
                    var parameters = string.Join(", ", Parameters.Select(x => x.ToTypeDisplayString()));
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

        var parameters = string.Join(", ", Parameters.Select(x => x.ToTypeDisplayString()));
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
    public required Location Location { get; init; }
    public required bool IsNullableReference { get; init; }
    public required bool IsParams { get; init; }
    public required string Name { get; init; }
    public required string OriginalParameterName { get; init; }
    public required bool HasDefaultValue { get; init; }
    public object? DefaultValue { get; init; }
    public required ITypeSymbol? CustomParserType { get; init; }
    public required bool IsFromServices { get; init; }
    public required bool IsConsoleAppContext { get; init; }
    public required bool IsCancellationToken { get; init; }
    public bool IsParsable => !(IsFromServices || IsCancellationToken || IsConsoleAppContext);
    public bool IsFlag => Type.SpecialType == SpecialType.System_Boolean;
    public required bool HasValidation { get; init; }
    public required int ArgumentIndex { get; init; } // -1 is not Argument, other than marked as [Argument]
    public bool IsArgument => ArgumentIndex != -1;
    public required string[] Aliases { get; init; }
    public required string Description { get; init; }
    public bool RequireCheckArgumentParsed => !(HasDefaultValue || IsParams || IsFlag);

    // increment = false when passed from [Argument]
    public string BuildParseMethod(int argCount, string argumentName, WellKnownTypes wellKnownTypes, bool increment)
    {
        var incrementIndex = increment ? "!TryIncrementIndex(ref i, args.Length) || " : "";
        return Core(Type, false);

        string Core(ITypeSymbol type, bool nullable)
        {
            var tryParseKnownPrimitive = false;
            var tryParseIParsable = false;

            var outArgVar = (!nullable) ? $"out arg{argCount}" : $"out var temp{argCount}";
            var elseExpr = (!nullable) ? "" : $" else {{ arg{argCount} = temp{argCount}; }}";

            // Nullable
            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                var valueType = (type as INamedTypeSymbol)!.TypeArguments[0];
                return Core(valueType, true);
            }

            if (CustomParserType != null)
            {
                return $"if ({incrementIndex}!{CustomParserType.ToFullyQualifiedFormatDisplayString()}.TryParse(args[i], {outArgVar})) {{ ThrowArgumentParseFailed(\"{argumentName}\", args[i]); }}{elseExpr}";
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_String:
                    // no parse
                    if (increment)
                    {
                        return $"if (!TryIncrementIndex(ref i, args.Length)) {{ ThrowArgumentParseFailed(\"{argumentName}\", args[i]); }} else {{ arg{argCount} = args[i]; }}";
                    }
                    else
                    {
                        return $"arg{argCount} = args[i];";
                    }
                    
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
                    if (type.TypeKind == TypeKind.Enum)
                    {
                        return $"if ({incrementIndex}!Enum.TryParse<{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(args[i], true, {outArgVar})) {{ ThrowArgumentParseFailed(\"{argumentName}\", args[i]); }}{elseExpr}";
                    }

                    // ParamsArray
                    if (IsParams)
                    {
                        return $"{(increment ? "i++; " : "")}if (!TryParseParamsArray(args, ref arg{argCount}, ref i)) {{ ThrowArgumentParseFailed(\"{argumentName}\", args[i]); }}{elseExpr}";
                    }

                    // Array
                    if (type.TypeKind == TypeKind.Array)
                    {
                        var elementType = (type as IArrayTypeSymbol)!.ElementType;
                        var parsable = wellKnownTypes.ISpanParsable;
                        if (parsable != null) // has parsable
                        {
                            if (elementType.AllInterfaces.Any(x => x.EqualsUnconstructedGenericType(parsable)))
                            {
                                return $"if ({incrementIndex}!TrySplitParse(args[i], {outArgVar})) {{ ThrowArgumentParseFailed(\"{argumentName}\", args[i]); }}{elseExpr}";
                            }
                        }
                        break;
                    }

                    // System.DateTimeOffset, System.Guid,  System.Version
                    tryParseKnownPrimitive = wellKnownTypes.HasTryParse(type);

                    if (!tryParseKnownPrimitive)
                    {
                        // ISpanParsable<T> (BigInteger, Complex, Half, Int128, etc...)
                        var parsable = wellKnownTypes.ISpanParsable;
                        if (parsable != null) // has parsable
                        {
                            tryParseIParsable = type.AllInterfaces.Any(x => x.EqualsUnconstructedGenericType(parsable));
                        }
                    }

                    break;
            }

            if (tryParseKnownPrimitive)
            {
                return $"if ({incrementIndex}!{type.ToFullyQualifiedFormatDisplayString()}.TryParse(args[i], {outArgVar})) {{ ThrowArgumentParseFailed(\"{argumentName}\", args[i]); }}{elseExpr}";
            }
            else if (tryParseIParsable)
            {
                return $"if ({incrementIndex}!{type.ToFullyQualifiedFormatDisplayString()}.TryParse(args[i], null, {outArgVar})) {{ ThrowArgumentParseFailed(\"{argumentName}\", args[i]); }}{elseExpr}";
            }
            else
            {
                return $"try {{ arg{argCount} = System.Text.Json.JsonSerializer.Deserialize<{type.ToFullyQualifiedFormatDisplayString()}>(args[{(increment ? "++i" : "i")}]); }} catch {{ ThrowArgumentParseFailed(\"{argumentName}\", args[i]); }}";
            }
        }
    }

    public string DefaultValueToString(bool castValue = true, bool enumIncludeTypeName = true)
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
            // null -> default(T) to support both class and struct
            return $"default({Type.ToFullyQualifiedFormatDisplayString()})";
        }
        if (Type.TypeKind == TypeKind.Enum)
        {
            var symbol = Type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => object.Equals(x.ConstantValue, DefaultValue));
            if (symbol == null)
            {
                return $"default({Type.ToFullyQualifiedFormatDisplayString()})";
            }
            else
            {
                return enumIncludeTypeName ? $"{Type.ToFullyQualifiedFormatDisplayString()}.{symbol.Name}" : symbol.Name;
            }
        }

        if (!castValue) return DefaultValue.ToString();
        return $"({Type.ToFullyQualifiedFormatDisplayString()}){DefaultValue}";
    }

    public string? GetEnumSymbolName(object value)
    {
        var symbol = Type.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(x => x.ConstantValue == value);
        if (symbol == null) return "";
        return symbol.Name;
    }

    public string ToTypeDisplayString()
    {
        var t = Type.ToFullyQualifiedFormatDisplayString();
        return IsNullableReference ? $"{t}?" : t;
    }

    public string ToTypeShortString()
    {
        var t = Type.ToDisplayString(NullableFlowState.NotNull, SymbolDisplayFormat.MinimallyQualifiedFormat);
        return IsNullableReference ? $"{t}?" : t;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        if (IsParams)
        {
            sb.Append("params ");
        }
        sb.Append(ToTypeDisplayString());
        sb.Append(" ");
        sb.Append(OriginalParameterName);
        if (HasDefaultValue)
        {
            sb.Append(" = ");
            sb.Append(DefaultValueToString(castValue: false));
        }

        return sb.ToString();
    }
}

public record class CommandMethodInfo
{
    public required string TypeFullName { get; init; }
    public required string MethodName { get; init; }
    public required ITypeSymbol[] ConstructorParameterTypes { get; init; }
    public required bool IsIDisposable { get; init; }
    public required bool IsIAsyncDisposable { get; init; }

    public string BuildNew()
    {
        var p = ConstructorParameterTypes.Select(parameter =>
        {
            var type = parameter.ToFullyQualifiedFormatDisplayString();
            return $"({type})ServiceProvider!.GetService(typeof({type}))!";
        });

        return $"new {TypeFullName}({string.Join(", ", p)})";
    }
}

public record class FilterInfo
{
    public required string TypeFullName { get; init; }
    public required ITypeSymbol[] ConstructorParameterTypes { get; init; }

    FilterInfo()
    {

    }

    public static FilterInfo? Create(ITypeSymbol type)
    {
        var publicConstructors = type.GetMembers()
             .OfType<IMethodSymbol>()
             .Where(x => x.MethodKind == Microsoft.CodeAnalysis.MethodKind.Constructor && x.DeclaredAccessibility == Accessibility.Public)
             .ToArray();

        if (publicConstructors.Length != 1)
        {
            return null;
        }

        var filter = new FilterInfo
        {
            TypeFullName = type.ToFullyQualifiedFormatDisplayString(),
            ConstructorParameterTypes = publicConstructors[0].Parameters.Select(x => x.Type).ToArray()
        };

        return filter;
    }

    public string BuildNew(string nextFilterName)
    {
        var p = ConstructorParameterTypes.Select(parameter =>
        {
            var type = parameter.ToFullyQualifiedFormatDisplayString();
            if (type.Contains("ConsoleAppFramework.ConsoleAppFilter"))
            {
                return nextFilterName;
            }
            else
            {
                return $"({type})ServiceProvider!.GetService(typeof({type}))!";
            }
        });

        return $"new {TypeFullName}({string.Join(", ", p)})";
    }
}