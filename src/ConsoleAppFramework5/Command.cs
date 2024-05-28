using Microsoft.CodeAnalysis;
using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
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
    public string CommandFullName => (CommandPath.Length == 0) ? CommandName : $"{string.Join("/", CommandPath)}/{CommandName}";
    public bool IsRootCommand => CommandFullName == "";
    public required string[] CommandPath { get; init; }
    public required string CommandName { get; init; }
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
            if (MethodKind == MethodKind.Lambda && Parameters.Any(x => x.HasDefaultValue))
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
    public required string Name { get; init; }
    public required bool HasDefaultValue { get; init; }
    public object? DefaultValue { get; init; }
    public required ITypeSymbol? CustomParserType { get; init; }
    public required bool IsFromServices { get; init; }
    public required bool IsCancellationToken { get; init; }
    public bool IsParsable => !(IsFromServices || IsCancellationToken);
    public required bool HasValidation { get; init; }
    public required int ArgumentIndex { get; init; } // -1 is not Argument, other than marked as [Argument]
    public bool IsArgument => ArgumentIndex != -1;
    public required string[] Aliases { get; init; }
    public required string Description { get; init; }

    public string BuildParseMethod(int argCount, string argumentName, WellKnownTypes wellKnownTypes, bool increment)
    {
        var index = increment ? "++i" : "i";
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
                return $"if (!{CustomParserType.ToFullyQualifiedFormatDisplayString()}.TryParse(args[{index}], {outArgVar})) {{ ThrowArgumentParseFailed(\"{argumentName}\", args[i]); }}{elseExpr}";
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_String:
                    return $"arg{argCount} = args[{index}];"; // no parse
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
                        return $"if (!Enum.TryParse<{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(args[{index}], true, {outArgVar})) {{ ThrowArgumentParseFailed(\"{argumentName}\", args[i]); }}{elseExpr}";
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
                                return $"if (!TrySplitParse(args[{index}], {outArgVar})) {{ ThrowArgumentParseFailed(\"{argumentName}\", args[i]); }}{elseExpr}";
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
                return $"if (!{type.ToFullyQualifiedFormatDisplayString()}.TryParse(args[{index}], {outArgVar})) {{ ThrowArgumentParseFailed(\"{argumentName}\", args[i]); }}{elseExpr}";
            }
            else if (tryParseIParsable)
            {
                return $"if (!{type.ToFullyQualifiedFormatDisplayString()}.TryParse(args[{index}], null, {outArgVar})) {{ ThrowArgumentParseFailed(\"{argumentName}\", args[i]); }}{elseExpr}";
            }
            else
            {
                return $"try {{ arg{argCount} = System.Text.Json.JsonSerializer.Deserialize<{type.ToFullyQualifiedFormatDisplayString()}>(args[{index}]); }} catch {{ ThrowArgumentParseFailed(\"{argumentName}\", args[i]); }}";
            }
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
            // null -> default(T) to support both class and struct
            return $"default({Type.ToFullyQualifiedFormatDisplayString()})";
        }

        if (!castValue) return DefaultValue.ToString();
        return $"({Type.ToFullyQualifiedFormatDisplayString()}){DefaultValue}";
    }

    public string ToTypeDisplayString()
    {
        var t = Type.ToFullyQualifiedFormatDisplayString();
        return IsNullableReference ? $"{t}?" : t;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(ToTypeDisplayString());
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