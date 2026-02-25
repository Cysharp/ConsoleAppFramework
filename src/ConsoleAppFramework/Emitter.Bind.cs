using Microsoft.CodeAnalysis;

namespace ConsoleAppFramework;

/// <summary>
/// Determines the parse code generation mode for different contexts.
/// </summary>
/// <remarks>
/// The three modes reflect where in the parsing pipeline the code runs:
/// <list type="bullet">
/// <item><description><see cref="Argument"/>: Direct value access at commandArgs[i], strict validation with immediate throw on parse failure. Used for positional arguments where we know the exact index.</description></item>
/// <item><description><see cref="Option"/>: Uses TryIncrementIndex to safely advance past option name to value, throws on failure. Standard parsing for named options like --port 8080.</description></item>
/// <item><description><see cref="GlobalOption"/>: Silent skip on bounds/parse failure (inline ++i). Used for pre-command parsing where unrecognized options are passed through to command handlers.</description></item>
/// </list>
/// </remarks>
internal enum ParseMode
{
    /// <summary>Argument: direct access to commandArgs[i], throws on parse failure.</summary>
    Argument,
    /// <summary>Option: uses TryIncrementIndex, throws on parse failure.</summary>
    Option,
    /// <summary>GlobalOption: inline ++i, silently ignores parse failures.</summary>
    GlobalOption
}

/// <summary>
/// Categorized properties for object construction, separating required, optional, and inherited global options.
/// </summary>
/// <param name="Required">Properties that must be initialized (marked required or arguments without defaults).</param>
/// <param name="Optional">Properties with defaults that are conditionally assigned if parsed.</param>
/// <param name="GlobalOptions">Properties inherited from a [GlobalOptions] base type (copied from typedGlobalOptions).</param>
internal record CategorizedProperties(
    IReadOnlyCollection<BindablePropertyInfo> Required,
    IReadOnlyCollection<BindablePropertyInfo> Optional,
    IReadOnlyCollection<BindablePropertyInfo> GlobalOptions);

internal partial class Emitter
{
    // [Bind] helper methods

    /// <summary>
    /// Determines if a property should be skipped during processing.
    /// </summary>
    /// <param name="prop">The property to check.</param>
    /// <param name="skipArguments">Whether to skip argument properties.</param>
    /// <returns>True if the property should be skipped.</returns>
    static bool ShouldSkipProperty(BindablePropertyInfo prop, bool skipArguments = false)
    {
        // Skip nested properties (not supported in this version)
        if (prop.ParentPath.Length > 0) return true;
        // Skip properties from global options (handled separately)
        if (prop.IsFromGlobalOptions) return true;
        // Optionally skip arguments (parsed in an argument section)
        if (skipArguments && prop.IsArgument) return true;
        return false;
    }

    void EmitBoundVariableDeclarations(SourceBuilder sb, CommandParameter parameter, int paramIndex)
    {
        var binding = parameter.ObjectBinding!;

        // Emit variable declarations for each bindable property (including arguments)
        foreach (var prop in binding.Properties)
        {
            if (ShouldSkipProperty(prop)) continue;

            var varName = GetBindPropertyVarName(paramIndex, prop);
            var typeFullName = prop.Type.ToFullyQualifiedFormatDisplayString();

            // Get default value
            var defaultValue = prop is { HasDefaultValue: true, DefaultValue: not null }
                ? FormatDefaultValue(prop.DefaultValue, prop.Type.TypeSymbol)
                : $"default({typeFullName})!";

            sb.AppendLine($"var {varName} = {defaultValue};");

            // Emit a parsed flag for:
            // - All non-constructor properties (needed for conditional assignment)
            // - Required constructor properties (needed for validation)
            // - Arguments (needed for validation)
            if (!prop.IsConstructorParameter || prop.IsRequired || prop.IsArgument)
            {
                sb.AppendLine($"var {varName}Parsed = false;");
            }
        }
    }

    void EmitBoundSwitchCases(SourceBuilder sb, CommandParameter parameter, int paramIndex)
    {
        var binding = parameter.ObjectBinding!;

        foreach (var prop in binding.Properties)
        {
            if (ShouldSkipProperty(prop, skipArguments: true)) continue;

            var varName = GetBindPropertyVarName(paramIndex, prop);
            var cliName = prop.CliName;

            // Add CliName case (only if not already in aliases)
            if (!prop.Aliases.Contains(cliName))
            {
                sb.AppendLine($"case \"{cliName}\":");
            }
            // Add aliases as case labels
            foreach (var alias in prop.Aliases)
            {
                sb.AppendLine($"case \"{alias}\":");
            }

            using var block = sb.BeginBlock();
            EmitPropertyParseCode(sb, prop, varName);
            // Always set a parsed flag (needed for conditional assignment)
            if (!prop.IsConstructorParameter || prop.IsRequired)
            {
                sb.AppendLine($"{varName}Parsed = true;");
            }
            sb.AppendLine("continue;");
        }
    }

    void EmitBoundCaseInsensitiveCases(SourceBuilder sb, CommandParameter parameter, int paramIndex)
    {
        var binding = parameter.ObjectBinding!;

        foreach (var prop in binding.Properties)
        {
            if (ShouldSkipProperty(prop, skipArguments: true)) continue;

            var varName = GetBindPropertyVarName(paramIndex, prop);
            var cliName = prop.CliName;

            // Build condition including aliases, avoiding duplicates
            var allOptions = new List<string>();
            if (!prop.Aliases.Contains(cliName))
            {
                allOptions.Add(cliName);
            }
            allOptions.AddRange(prop.Aliases);

            sb.AppendLine($"if (string.Equals(name, \"{allOptions[0]}\", StringComparison.OrdinalIgnoreCase){(allOptions.Count == 1 ? ")" : "")}");
            for (int j = 1; j < allOptions.Count; j++)
            {
                sb.AppendLine($" || string.Equals(name, \"{allOptions[j]}\", StringComparison.OrdinalIgnoreCase){(allOptions.Count == j + 1 ? ")" : "")}");
            }

            using var block = sb.BeginBlock();
            EmitPropertyParseCode(sb, prop, varName);
            // Always set a parsed flag (needed for conditional assignment)
            if (!prop.IsConstructorParameter || prop.IsRequired)
            {
                sb.AppendLine($"{varName}Parsed = true;");
            }
            sb.AppendLine("continue;");
        }
    }

    void EmitBoundArgumentParsing(SourceBuilder sb, CommandParameter parameter, int paramIndex, int baseArgumentIndex)
    {
        var binding = parameter.ObjectBinding!;

        // Get all argument properties sorted by index (excluding global options properties)
        var argumentProps = binding.Properties
            .Where(p => p.IsArgument && p.ParentPath.Length == 0 && !p.IsFromGlobalOptions)
            .OrderBy(p => p.ArgumentIndex)
            .ToArray();

        foreach (var prop in argumentProps)
        {
            var varName = GetBindPropertyVarName(paramIndex, prop);
            var globalArgIndex = baseArgumentIndex + prop.ArgumentIndex;

            sb.AppendLine($"if (argumentPosition == {globalArgIndex})");
            using var block = sb.BeginBlock();
            EmitArgumentPropertyParseCode(sb, prop, varName);
            sb.AppendLine($"{varName}Parsed = true;");
            sb.AppendLine("argumentPosition++;");
            sb.AppendLine("continue;");
        }
    }

    void EmitArgumentPropertyParseCode(SourceBuilder sb, BindablePropertyInfo prop, string varName)
    {
        var argName = $"[{prop.ArgumentIndex}]";
        EmitTypeParseCodeCore(sb, prop, varName, argName, ParseMode.Argument, nullable: false);
    }

    void EmitPropertyParseCode(SourceBuilder sb, BindablePropertyInfo prop, string varName)
    {
        var argName = prop.CliName.TrimStart('-');
        EmitTypeParseCodeCore(sb, prop, varName, argName, ParseMode.Option, nullable: false);
    }

    /// <summary>
    /// Unified method for emitting type parsing code across different contexts.
    /// </summary>
    /// <remarks>
    /// For Nullable&lt;T&gt; types, we detect them and set nullable=true for the temp variable pattern.
    /// When nullable=true, we use a temporary variable (temp_varName) with TryParse to avoid
    /// overwriting the target variable on parse failure - we only assign on success via the else clause.
    /// </remarks>
    void EmitTypeParseCodeCore(SourceBuilder sb, BindablePropertyInfo prop, string varName, string argName, ParseMode mode, bool nullable)
    {
        var type = prop.Type.TypeSymbol;

        // Detect Nullable<T> and set nullable flag (ParseInfo already has the unwrapped type info)
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            nullable = true;
        }

        // When nullable=true, use a temp variable to avoid overwriting the target on parse failure
        var outArgVar = (!nullable) ? $"out {varName}" : $"out var temp_{varName}";
        var elseExpr = (!nullable) ? "" : $" else {{ {varName} = temp_{varName}; }}";

        // Use the pre-computed ParseInfo from the property (already unwrapped for Nullable<T>)
        var parseInfo = prop.ParseInfo;

        switch (parseInfo.Category)
        {
            case ParseCategory.String:
                EmitStringParse(sb, varName, argName, mode);
                return;

            case ParseCategory.Boolean:
                EmitBooleanParse(sb, varName, argName, mode, outArgVar, elseExpr);
                return;

            case ParseCategory.Primitive:
                EmitTryParseable(sb, parseInfo.FullTypeName, argName, mode, outArgVar, elseExpr, isEnum: false);
                return;

            case ParseCategory.Enum:
                EmitTryParseable(sb, parseInfo.FullTypeName, argName, mode, outArgVar, elseExpr, isEnum: true);
                return;

            case ParseCategory.SpanParsable:
                EmitSpanParsableParse(sb, parseInfo, argName, mode, outArgVar, elseExpr);
                return;

            case ParseCategory.Array:
                EmitArrayParse(sb, parseInfo, argName, mode, outArgVar, elseExpr);
                return;

            case ParseCategory.Json:
                EmitJsonParse(sb, parseInfo, varName, argName, mode);
                return;
        }
    }

    static void EmitBooleanParse(SourceBuilder sb, string varName, string argName, ParseMode mode, string outArgVar, string elseExpr)
    {
        switch (mode)
        {
            case ParseMode.Argument:
                // Argument: parse "true"/"false" string
                sb.AppendLine($"if (!bool.TryParse(commandArgs[i], {outArgVar})) {{ ThrowArgumentParseFailed(\"{argName}\", commandArgs[i]); }}{elseExpr}");
                break;
            case ParseMode.Option:
            case ParseMode.GlobalOption:
                // Option/GlobalOption: presence means true (flag)
                sb.AppendLine($"{varName} = true;");
                break;
        }
    }

    static void EmitStringParse(SourceBuilder sb, string varName, string argName, ParseMode mode)
    {
        switch (mode)
        {
            case ParseMode.Argument:
                sb.AppendLine($"{varName} = commandArgs[i];");
                break;
            case ParseMode.Option:
                sb.AppendLine($"if (!TryIncrementIndex(ref i, commandArgs.Length)) {{ ThrowArgumentParseFailed(\"{argName}\", commandArgs[i]); }} else {{ {varName} = commandArgs[i]; }}");
                break;
            case ParseMode.GlobalOption:
                sb.AppendLine($"if (++i < commandArgs.Length) {{ {varName} = commandArgs[i]; }}");
                break;
        }
    }

    static void EmitTryParseable(SourceBuilder sb, string typeName, string argName, ParseMode mode, string outArgVar, string elseExpr, bool isEnum)
    {
        var parseExpr = isEnum
            ? $"Enum.TryParse<{typeName}>(commandArgs[i], true, {outArgVar})"
            : $"{typeName}.TryParse(commandArgs[i], {outArgVar})";

        var throwExpr = isEnum
            ? $"ThrowArgumentParseFailedEnum(typeof({typeName}), \"{argName}\", commandArgs[i])"
            : $"ThrowArgumentParseFailed(\"{argName}\", commandArgs[i])";

        switch (mode)
        {
            case ParseMode.Argument:
                sb.AppendLine($"if (!{parseExpr}) {{ {throwExpr}; }}{elseExpr}");
                break;
            case ParseMode.Option:
                sb.AppendLine($"if (!TryIncrementIndex(ref i, commandArgs.Length) || !{parseExpr}) {{ {throwExpr}; }}{elseExpr}");
                break;
            case ParseMode.GlobalOption:
                sb.AppendLine($"if (++i < commandArgs.Length && {parseExpr}) {{ }}{elseExpr}");
                break;
        }
    }

    static void EmitSpanParsableParse(SourceBuilder sb, ParseInfo info, string argName, ParseMode mode, string outArgVar, string elseExpr)
    {
        // ISpanParsable<T>.TryParse(string, IFormatProvider?, out T)
        var parseExpr = $"{info.FullTypeName}.TryParse(commandArgs[i], null, {outArgVar})";
        var throwExpr = $"ThrowArgumentParseFailed(\"{argName}\", commandArgs[i])";

        switch (mode)
        {
            case ParseMode.Argument:
                sb.AppendLine($"if (!{parseExpr}) {{ {throwExpr}; }}{elseExpr}");
                break;
            case ParseMode.Option:
                sb.AppendLine($"if (!TryIncrementIndex(ref i, commandArgs.Length) || !{parseExpr}) {{ {throwExpr}; }}{elseExpr}");
                break;
            case ParseMode.GlobalOption:
                sb.AppendLine($"if (++i < commandArgs.Length && {parseExpr}) {{ }}{elseExpr}");
                break;
        }
    }

    static void EmitArrayParse(SourceBuilder sb, ParseInfo info, string argName, ParseMode mode, string outArgVar, string elseExpr)
    {
        // Arrays use TrySplitParse<T> which splits on comma and parses each element
        var parseExpr = $"TrySplitParse(commandArgs[i], {outArgVar})";
        var throwExpr = $"ThrowArgumentParseFailed(\"{argName}\", commandArgs[i])";

        switch (mode)
        {
            case ParseMode.Argument:
                sb.AppendLine($"if (!{parseExpr}) {{ {throwExpr}; }}{elseExpr}");
                break;
            case ParseMode.Option:
                sb.AppendLine($"if (!TryIncrementIndex(ref i, commandArgs.Length) || !{parseExpr}) {{ {throwExpr}; }}{elseExpr}");
                break;
            case ParseMode.GlobalOption:
                sb.AppendLine($"if (++i < commandArgs.Length && {parseExpr}) {{ }}{elseExpr}");
                break;
        }
    }

    static void EmitJsonParse(SourceBuilder sb, ParseInfo info, string varName, string argName, ParseMode mode)
    {
        // JSON fallback for complex types
        switch (mode)
        {
            case ParseMode.Argument:
                sb.AppendLine($"try {{ {varName} = System.Text.Json.JsonSerializer.Deserialize<{info.FullTypeName}>(commandArgs[i], JsonSerializerOptions); }} catch {{ ThrowArgumentParseFailed(\"{argName}\", commandArgs[i]); }}");
                break;
            case ParseMode.Option:
                sb.AppendLine($"if (!TryIncrementIndex(ref i, commandArgs.Length)) {{ ThrowArgumentParseFailed(\"{argName}\", commandArgs[i]); }}");
                sb.AppendLine($"try {{ {varName} = System.Text.Json.JsonSerializer.Deserialize<{info.FullTypeName}>(commandArgs[i], JsonSerializerOptions); }} catch {{ ThrowArgumentParseFailed(\"{argName}\", commandArgs[i]); }}");
                break;
            case ParseMode.GlobalOption:
                sb.AppendLine($"if (++i < commandArgs.Length) {{ try {{ {varName} = System.Text.Json.JsonSerializer.Deserialize<{info.FullTypeName}>(commandArgs[i], JsonSerializerOptions); }} catch {{ }} }}");
                break;
        }
    }

    static void EmitBoundValidation(SourceBuilder sb, CommandParameter parameter, int paramIndex)
    {
        var binding = parameter.ObjectBinding!;

        foreach (var prop in binding.Properties)
        {
            if (ShouldSkipProperty(prop)) continue;

            // Arguments without defaults are required; arguments with defaults are optional
            var isRequiredArg = prop.IsArgument && !prop.HasDefaultValue;
            if (prop.IsRequired || isRequiredArg)
            {
                var varName = GetBindPropertyVarName(paramIndex, prop);
                var argName = prop.IsArgument ? $"[{prop.ArgumentIndex}]" : prop.CliName.TrimStart('-');
                sb.AppendLine($"if (!{varName}Parsed) ThrowRequiredArgumentNotParsed(\"{argName}\");");
            }
        }
    }

    void EmitBoundObjectConstruction(SourceBuilder sb, CommandParameter parameter, int paramIndex)
    {
        var binding = parameter.ObjectBinding!;
        var typeFullName = binding.BoundType.ToFullyQualifiedFormatDisplayString();
        var isRecord = binding.BoundType.TypeSymbol.IsRecord;
        var hasGlobalOptionsInheritance = binding.GlobalOptionsBaseType != null && typedGlobalOptions != null;

        if (binding.HasPrimaryConstructor)
        {
            EmitObjectConstructionWithPrimaryConstructor(sb, binding, typeFullName, isRecord, hasGlobalOptionsInheritance, paramIndex);
        }
        else
        {
            EmitObjectConstructionWithParameterlessConstructor(sb, binding, typeFullName, isRecord, hasGlobalOptionsInheritance, paramIndex);
        }
    }

    void EmitObjectConstructionWithPrimaryConstructor(
        SourceBuilder sb, ObjectBindingInfo binding, string typeFullName, bool isRecord,
        bool hasGlobalOptionsInheritance, int paramIndex)
    {
        // Build constructor arguments
        var ctorArgs = new List<string>();
        foreach (var ctorParam in binding.ConstructorParameters)
        {
            var prop = binding.Properties.FirstOrDefault(p =>
                p.IsConstructorParameter && p.ConstructorParameterIndex == ctorParam.Index);
            if (prop != null)
            {
                ctorArgs.Add(GetPropertyValueSource(prop, hasGlobalOptionsInheritance, paramIndex));
            }
            else
            {
                var defaultVal = ctorParam.HasDefaultValue
                    ? FormatDefaultValue(ctorParam.DefaultValue, ctorParam.Type.TypeSymbol)
                    : $"default({ctorParam.Type.ToFullyQualifiedFormatDisplayString()})!";
                ctorArgs.Add(defaultVal);
            }
        }

        var categorized = CategorizeProperties(binding.Properties, hasGlobalOptionsInheritance, skipConstructorParams: true);

        var ctorArgsStr = string.Join(", ", ctorArgs);
        var allInits = BuildPropertyInitializers(categorized.Required, categorized.GlobalOptions, paramIndex);

        sb.AppendLine(allInits.Count > 0
            ? $"var arg{paramIndex} = new {typeFullName}({ctorArgsStr}) {{ {string.Join(", ", allInits)} }};"
            : $"var arg{paramIndex} = new {typeFullName}({ctorArgsStr});"
        );

        EmitOptionalPropertyAssignments(sb, categorized.Optional, isRecord, paramIndex);
    }

    void EmitObjectConstructionWithParameterlessConstructor(
        SourceBuilder sb, ObjectBindingInfo binding, string typeFullName, bool isRecord,
        bool hasGlobalOptionsInheritance, int paramIndex)
    {
        var categorized = CategorizeProperties(binding.Properties, hasGlobalOptionsInheritance, skipConstructorParams: false);

        var allInits = BuildPropertyInitializers(categorized.Required, categorized.GlobalOptions, paramIndex);

        sb.AppendLine(allInits.Count > 0
            ? $"var arg{paramIndex} = new {typeFullName}() {{ {string.Join(", ", allInits)} }};"
            : $"var arg{paramIndex} = new {typeFullName}();"
        );

        EmitOptionalPropertyAssignments(sb, categorized.Optional, isRecord, paramIndex);
    }

    static string GetPropertyValueSource(BindablePropertyInfo prop, bool hasGlobalOptionsInheritance, int paramIndex) =>
        prop.IsFromGlobalOptions && hasGlobalOptionsInheritance
            ? $"typedGlobalOptions.{prop.PropertyName}"
            : GetBindPropertyVarName(paramIndex, prop);

    static CategorizedProperties CategorizeProperties(
        EquatableArray<BindablePropertyInfo> properties, bool hasGlobalOptionsInheritance, bool skipConstructorParams)
    {
        var required = new List<BindablePropertyInfo>();
        var optional = new List<BindablePropertyInfo>();
        var globalOptions = new List<BindablePropertyInfo>();

        foreach (var prop in properties)
        {
            if (skipConstructorParams && prop.IsConstructorParameter) continue;
            if (prop.ParentPath.Length > 0) continue;

            if (prop.IsFromGlobalOptions && hasGlobalOptionsInheritance)
            {
                globalOptions.Add(prop);
                continue;
            }

            var isRequiredArg = prop is { IsArgument: true, HasDefaultValue: false };
            if (prop.IsRequired || isRequiredArg)
            {
                required.Add(prop);
            }
            else
            {
                optional.Add(prop);
            }
        }

        return new CategorizedProperties(required, optional, globalOptions);
    }

    static List<string> BuildPropertyInitializers(
        IReadOnlyCollection<BindablePropertyInfo> requiredProps,
        IReadOnlyCollection<BindablePropertyInfo> globalOptionsProps,
        int paramIndex
    )
    {
        var inits = new List<string>();
        inits.AddRange(requiredProps.Select(p => $"{p.PropertyName} = {GetBindPropertyVarName(paramIndex, p)}"));
        inits.AddRange(globalOptionsProps.Select(p => $"{p.PropertyName} = typedGlobalOptions.{p.PropertyName}"));
        return inits;
    }

    void EmitOptionalPropertyAssignments(
        SourceBuilder sb, IReadOnlyCollection<BindablePropertyInfo> optionalProps, bool isRecord, int paramIndex)
    {
        foreach (var prop in optionalProps)
        {
            var varName = GetBindPropertyVarName(paramIndex, prop);
            if (isRecord && prop.IsInitOnly)
            {
                sb.AppendLine($"if ({varName}Parsed) arg{paramIndex} = arg{paramIndex} with {{ {prop.PropertyName} = {varName} }};");
            }
            else
            {
                sb.AppendLine($"if ({varName}Parsed) arg{paramIndex}.{prop.PropertyName} = {varName};");
            }
        }
    }

    static string GetBindPropertyVarName(int paramIndex, BindablePropertyInfo prop)
    {
        // Create a unique variable name based on parameter index and property path
        var sanitizedName = prop.PropertyAccessPath.Replace(".", "_");
        return $"arg{paramIndex}_{sanitizedName}";
    }

    static string FormatDefaultValue(object? value, ITypeSymbol type)
    {
        switch (value)
        {
            case null:
                return "null";
            case string s:
                return $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
            case bool b:
                return b ? "true" : "false";
            case char c:
                return $"'{c}'";
        }

        if (type.TypeKind == TypeKind.Enum)
            return $"({type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){value}";

        // For numeric types, use the target type to determine the correct suffix/cast
        // because the literal value might be stored as a different type (e.g., 0 is int even for ulong)
        switch (type.SpecialType)
        {
            case SpecialType.System_Decimal:
                return $"{value}m";
            case SpecialType.System_Single:
                return $"{value}f";
            case SpecialType.System_Double:
                return $"(double){value}";
            case SpecialType.System_Int64:
                return $"{value}L";
            case SpecialType.System_UInt64:
                return $"{value}UL";
            case SpecialType.System_UInt32:
                return $"{value}U";
            // Small integer types need explicit casts because there's no suffix
            case SpecialType.System_Byte:
                return $"(byte){value}";
            case SpecialType.System_SByte:
                return $"(sbyte){value}";
            case SpecialType.System_Int16:
                return $"(short){value}";
            case SpecialType.System_UInt16:
                return $"(ushort){value}";
            case SpecialType.System_Int32:
                // int is the default for integer literals
                return value.ToString() ?? "default";
        }

        return value.ToString() ?? "default";
    }
}
