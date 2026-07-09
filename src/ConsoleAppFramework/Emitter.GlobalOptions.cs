namespace ConsoleAppFramework;

internal partial class Emitter
{
    /// <summary>
    /// Emits code to parse typed global options BEFORE command routing.
    /// This allows global options to appear before the command name.
    /// </summary>
    void EmitTypedGlobalOptionsPreParsing(SourceBuilder sb)
    {
        if (typedGlobalOptions == null) return;

        sb.AppendLine("var (parsedOptions, remainingArgs) = ParseTypedGlobalOptions(args.AsMemory());");
        sb.AppendLine("typedGlobalOptions = parsedOptions;");
        sb.AppendLine("args = remainingArgs.ToArray();");
        sb.AppendLine();
        // Critical: Set configureGlobalOptions so RunWithFilterAsync can access typed global options
        sb.AppendLine("this.configureGlobalOptions = (ref GlobalOptionsBuilder _) => typedGlobalOptions!;");
        sb.AppendLine("this.isRequireCallBuildAndSetServiceProvider = true;");
        sb.AppendLine();
    }

    /// <summary>
    /// Emits the field declaration for typed global options if needed.
    /// </summary>
    void EmitTypedGlobalOptionsField(SourceBuilder sb)
    {
        if (typedGlobalOptions == null) return;

        var globalOptionsTypeName = typedGlobalOptions.Type.ToFullyQualifiedFormatDisplayString();
        sb.AppendLine($"{globalOptionsTypeName}? typedGlobalOptions;");
    }

    /// <summary>
    /// Emits the typed global options parser method.
    /// Generates code that parses global options from command args and returns remaining args.
    /// </summary>
    public void EmitTypedGlobalOptionsParsing(SourceBuilder sb, TypedGlobalOptionsInfo globalOptions)
    {
        var binding = globalOptions.ObjectBinding;
        var typeFullName = binding.BoundType.ToFullyQualifiedFormatDisplayString();

        sb.AppendLine();
        sb.AppendLine($"static ({typeFullName} globalOptions, ReadOnlyMemory<string> remainingArgs) ParseTypedGlobalOptions(ReadOnlyMemory<string> commandArgsMemory)");
        using (sb.BeginBlock())
        {
            // Create a default instance to get property initializer values
            // (property initializers aren't accessible at compile-time via Roslyn)
            sb.AppendLine($"var __defaults = new {typeFullName}();");
            sb.AppendLine();

            // Emit variable declarations for each property, copying defaults from the instance
            foreach (var prop in binding.Properties)
            {
                if (prop.ParentPath.Length > 0) continue;

                var varName = $"global_{prop.PropertyName}";

                // Use the default instance to get the actual property initializer value
                sb.AppendLine($"var {varName} = __defaults.{prop.PropertyName};");
            }

            sb.AppendLine();
            sb.AppendLine("var commandArgs = commandArgsMemory.Span;");
            sb.AppendLine("var remainingArgsList = new System.Collections.Generic.List<string>();");
            sb.AppendLine();

            using (sb.BeginBlock("for (int i = 0; i < commandArgs.Length; i++)"))
            {
                sb.AppendLine("var name = commandArgs[i];");
                sb.AppendLine("var consumed = false;");
                sb.AppendLine();

                // Filter to top-level, non-argument properties (shared by switch and case-insensitive fallback)
                var optionProperties = binding.Properties
                    .Where(p => p.ParentPath.Length == 0 && !p.IsArgument)
                    .ToArray();

                using (sb.BeginBlock("switch (name)"))
                {
                    // Emit switch cases for each property
                    foreach (var prop in optionProperties)
                    {
                        var varName = $"global_{prop.PropertyName}";
                        sb.AppendLine($"case \"{prop.CliName}\":");
                        using (sb.BeginBlock())
                        {
                            EmitGlobalPropertyParseCode(sb, prop, varName);
                            sb.AppendLine("consumed = true;");
                            sb.AppendLine("break;");
                        }
                    }

                    using (sb.BeginIndent("default:"))
                    {
                        // Case-insensitive fallback
                        foreach (var prop in optionProperties)
                        {
                            var varName = $"global_{prop.PropertyName}";
                            sb.AppendLine($"if (string.Equals(name, \"{prop.CliName}\", StringComparison.OrdinalIgnoreCase))");
                            using (sb.BeginBlock())
                            {
                                EmitGlobalPropertyParseCode(sb, prop, varName);
                                sb.AppendLine("consumed = true;");
                            }
                        }
                        sb.AppendLine("break;");
                    }
                }

                sb.AppendLine();
                using (sb.BeginBlock("if (!consumed)"))
                {
                    sb.AppendLine("remainingArgsList.Add(name);");
                }
            }

            sb.AppendLine();

            // Construct the global options object
            if (binding.HasPrimaryConstructor)
            {
                var ctorArgs = binding.ConstructorParameters
                    .Select(p =>
                    {
                        var prop = binding.Properties.FirstOrDefault(prop =>
                            prop.IsConstructorParameter && prop.ConstructorParameterIndex == p.Index);
                        return prop != null ? $"global_{prop.PropertyName}" : FormatDefaultValue(p.DefaultValue, p.Type.TypeSymbol);
                    })
                    .ToList();

                var nonCtorProps = binding.Properties
                    .Where(p => !p.IsConstructorParameter && p.ParentPath.Length == 0)
                    .ToList();

                if (nonCtorProps.Count > 0)
                {
                    var inits = nonCtorProps.Select(p => $"{p.PropertyName} = global_{p.PropertyName}");
                    sb.AppendLine($"var globalOptions = new {typeFullName}({string.Join(", ", ctorArgs)}) {{ {string.Join(", ", inits)} }};");
                }
                else
                {
                    sb.AppendLine($"var globalOptions = new {typeFullName}({string.Join(", ", ctorArgs)});");
                }
            }
            else
            {
                var inits = binding.Properties
                    .Where(p => p.ParentPath.Length == 0)
                    .Select(p => $"{p.PropertyName} = global_{p.PropertyName}");
                sb.AppendLine($"var globalOptions = new {typeFullName}() {{ {string.Join(", ", inits)} }};");
            }

            sb.AppendLine("return (globalOptions, remainingArgsList.ToArray());");
        }
    }

    void EmitGlobalPropertyParseCode(SourceBuilder sb, BindablePropertyInfo prop, string varName)
    {
        var argName = prop.CliName.TrimStart('-');
        EmitTypeParseCodeCore(sb, prop, varName, argName, ParseMode.GlobalOption, nullable: false);
    }
}
