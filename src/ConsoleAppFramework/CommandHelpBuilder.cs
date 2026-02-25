using Microsoft.CodeAnalysis;
using System.Text;

namespace ConsoleAppFramework;

public static class CommandHelpBuilder
{
    public static string BuildRootHelpMessage(Command command)
    {
        return BuildHelpMessageCore(command, showCommandName: false, showCommand: false);
    }

    public static string BuildRootHelpMessage(Command[] commands, TypedGlobalOptionsInfo? typedGlobalOptions = null)
    {
        var sb = new StringBuilder();

        var rootCommand = commands.FirstOrDefault(x => x.IsRootCommand);
        var withoutRoot = commands.Where(x => !x.IsRootCommand).ToArray();

        if (rootCommand != null && withoutRoot.Length == 0 && typedGlobalOptions == null)
        {
            return BuildRootHelpMessage(commands[0]);
        }

        if (rootCommand != null)
        {
            sb.AppendLine(BuildHelpMessageCore(rootCommand, false, withoutRoot.Length != 0));
        }
        else
        {
            sb.AppendLine("Usage: [command] [-h|--help] [--version]");
            sb.AppendLine();
        }

        // Add Global Options section if typed global options are configured
        if (typedGlobalOptions != null)
        {
            sb.AppendLine(BuildTypedGlobalOptionsMessage(typedGlobalOptions));
        }

        if (withoutRoot.Length == 0) return sb.ToString();

        var helpDefinitions = withoutRoot.OrderBy(x => x.Name).ToArray();

        var list = BuildMethodListMessage(helpDefinitions, out _);
        sb.Append(list);

        return sb.ToString();
    }

    public static string BuildTypedGlobalOptionsMessage(TypedGlobalOptionsInfo typedGlobalOptions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Global Options:");

        var optionsFormatted = typedGlobalOptions.ObjectBinding.Properties
            .Where(p => p.ParentPath.Length == 0 && !p.IsArgument)
            .Select(p =>
            {
                // Build option name with aliases (e.g., "-v, --verbose")
                var allOptions = new List<string>();
                foreach (var alias in p.Aliases)
                {
                    allOptions.Add(alias);
                }
                // Only add CliName if not already in aliases
                if (!p.Aliases.Contains(p.CliName))
                {
                    allOptions.Add(p.CliName);
                }
                var optionName = string.Join(", ", allOptions);

                var isFlag = p.IsFlag;
                var typeName = GetShortTypeName(p.Type.TypeSymbol);
                var formatted = isFlag ? optionName : $"{optionName} <{typeName}>";

                string? defaultValue = null;
                if (p.HasDefaultValue && p.DefaultValue != null)
                {
                    defaultValue = FormatDefaultValueForHelp(p.DefaultValue);
                    if (isFlag && p.DefaultValue is false)
                    {
                        defaultValue = null;
                    }
                }

                return (Option: formatted, Description: p.Description, IsFlag: isFlag, DefaultValue: defaultValue);
            })
            .ToArray();

        if (optionsFormatted.Length == 0) return "";

        var maxWidth = optionsFormatted.Max(x => x.Option.Length);

        var first = true;
        foreach (var opt in optionsFormatted)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                sb.AppendLine();
            }

            var padding = maxWidth - opt.Option.Length;
            sb.Append("  ");
            sb.Append(opt.Option);

            for (var i = 0; i < padding; i++)
            {
                sb.Append(' ');
            }

            sb.Append("    ");
            sb.Append(opt.Description);

            if (!opt.IsFlag && opt.DefaultValue != null)
            {
                sb.Append($" [default: {opt.DefaultValue}]");
            }
        }

        sb.AppendLine();
        return sb.ToString();
    }

    public static string BuildCommandHelpMessage(Command command)
    {
        return BuildHelpMessageCore(command, showCommandName: command.Name != "", showCommand: false);
    }

    public static string BuildCliSchema(IEnumerable<Command> commands)
    {
        return "return new CommandHelpDefinition[] {\n"
            + string.Join(", \n", commands.Select(x => CreateCommandHelpDefinition(x).ToCliSchema()))
            + "\n};";
    }

    static string BuildHelpMessageCore(Command command, bool showCommandName, bool showCommand)
    {
        var definition = CreateCommandHelpDefinition(command);

        var sb = new StringBuilder();

        sb.AppendLine(BuildUsageMessage(definition, showCommandName, showCommand));

        if (!string.IsNullOrEmpty(definition.Description))
        {
            sb.AppendLine();
            sb.AppendLine(definition.Description);
        }

        if (definition.Options.Any())
        {
            var hasArgument = definition.Options.Any(x => x.Index.HasValue);
            var hasNoHiddenOptions = definition.Options.Any(x => !x.Index.HasValue && !x.IsHidden);

            if (hasArgument)
            {
                sb.AppendLine();
                sb.AppendLine(BuildArgumentsMessage(definition));
            }

            if (hasNoHiddenOptions)
            {
                sb.AppendLine();
                sb.AppendLine(BuildOptionsMessage(definition));
            }
        }

        return sb.ToString();
    }

    static string BuildUsageMessage(CommandHelpDefinition definition, bool showCommandName, bool showCommand)
    {
        var sb = new StringBuilder();
        sb.Append($"Usage:");

        if (showCommandName)
        {
            sb.Append($" {definition.CommandName}");
        }

        if (showCommand)
        {
            sb.Append(" [command]");
        }

        if (definition.Options.Any(x => x.Index.HasValue))
        {
            sb.Append(" [arguments...]");
        }

        if (definition.Options.Any(x => !x.Index.HasValue && !x.IsHidden))
        {
            sb.Append(" [options...]");
        }

        sb.Append(" [-h|--help] [--version]");

        return sb.ToString();
    }

    static string BuildArgumentsMessage(CommandHelpDefinition definition)
    {
        var argumentsFormatted = definition.Options
            .Where(x => x.Index.HasValue)
            .Select(x => (Argument: $"[{x.Index}] {x.FormattedValueTypeName}", x.Description))
            .ToArray();

        if (!argumentsFormatted.Any()) return string.Empty;

        var maxWidth = argumentsFormatted.Max(x => x.Argument.Length);

        var sb = new StringBuilder();

        sb.AppendLine("Arguments:");
        var first = true;
        foreach (var arg in argumentsFormatted)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                sb.AppendLine();
            }
            var padding = maxWidth - arg.Argument.Length;

            sb.Append("  ");
            sb.Append(arg.Argument);
            if (!string.IsNullOrEmpty(arg.Description))
            {
                for (var i = 0; i < padding; i++)
                {
                    sb.Append(' ');
                }

                sb.Append("    ");
                sb.Append(arg.Description);
            }
        }

        return sb.ToString();
    }

    static string BuildOptionsMessage(CommandHelpDefinition definition)
    {
        var optionsFormatted = definition.Options
            .Where(x => !x.Index.HasValue)
            .Where(x => !x.IsHidden)
            .Select(x => (Options: string.Join(", ", x.Options) + (x.IsFlag ? string.Empty : $" {x.FormattedValueTypeName}{(x.IsParams ? "..." : "")}"), x.Description, x.IsRequired, x.IsFlag, x.DefaultValue, x.IsDefaultValueHidden))
            .ToArray();

        if (!optionsFormatted.Any()) return string.Empty;

        var maxWidth = optionsFormatted.Max(x => x.Options.Length);

        var sb = new StringBuilder();

        sb.AppendLine("Options:");
        var first = true;
        foreach (var opt in optionsFormatted)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                sb.AppendLine();
            }

            var options = opt.Options;
            var padding = maxWidth - options.Length;

            sb.Append("  ");
            sb.Append(options);
            for (var i = 0; i < padding; i++)
            {
                sb.Append(' ');
            }

            sb.Append("    ");
            sb.Append(opt.Description);

            // Flags are optional by default; leave them untagged.
            if (!opt.IsFlag)
            {
                if (opt.DefaultValue != null)
                {
                    if (!opt.IsDefaultValueHidden)
                    {
                        sb.Append($" [Default: {opt.DefaultValue}]");
                    }
                }
                else if (opt.IsRequired)
                {
                    sb.Append($" [Required]");
                }
            }
        }

        return sb.ToString();
    }

    static string BuildMethodListMessage(IEnumerable<Command> commands, out int maxWidth)
    {
        var formatted = commands
            .Where(x => !x.IsHidden)
            .Select(x =>
            {
                return (Command: string.Join(", ", x.Name.Split('|')), x.Description);
            })
            .ToArray();
        maxWidth = formatted.Max(x => x.Command.Length);

        var sb = new StringBuilder();

        sb.AppendLine("Commands:");
        foreach (var item in formatted)
        {
            sb.Append("  ");
            sb.Append(item.Command);
            if (string.IsNullOrEmpty(item.Description))
            {
                sb.AppendLine();
            }
            else
            {
                var padding = maxWidth - item.Command.Length;
                for (var i = 0; i < padding; i++)
                {
                    sb.Append(' ');
                }

                sb.Append("    ");
                sb.AppendLine(item.Description);
            }
        }

        return sb.ToString();
    }

    static CommandHelpDefinition CreateCommandHelpDefinition(Command descriptor)
    {
        var parameterDefinitions = new List<CommandOptionHelpDefinition>();

        foreach (var item in descriptor.Parameters)
        {
            // Handle [Bind] parameters by expanding their properties
            if (item.IsBound && item.ObjectBinding != null)
            {
                // Calculate base argument index for this [Bind] parameter
                // (after any preceding regular [Argument] params)
                var baseArgIndex = descriptor.Parameters
                    .TakeWhile(p => p != item)
                    .Where(p => p.IsArgument)
                    .Count();

                foreach (var prop in item.ObjectBinding.Properties)
                {
                    // Skip properties inherited from global options (they appear in Global Options section)
                    if (prop.IsFromGlobalOptions) continue;

                    var propOptions = new List<string>();
                    int? propIndex = null;

                    if (prop.IsArgument)
                    {
                        // This is a positional argument
                        var globalArgIndex = baseArgIndex + prop.ArgumentIndex;
                        propOptions.Add($"[{globalArgIndex}]");
                        propIndex = globalArgIndex;
                    }
                    else
                    {
                        // Add aliases first (e.g., -h before --host)
                        foreach (var alias in prop.Aliases)
                        {
                            propOptions.Add(alias);
                        }
                        // Only add CliName if not already in aliases
                        if (!prop.Aliases.Contains(prop.CliName))
                        {
                            propOptions.Add(prop.CliName);
                        }
                    }

                    var propDescription = prop.Description;
                    var propIsFlag = prop.IsFlag && !prop.IsArgument; // Arguments are never flags
                    var propIsParams = false;
                    var propIsHidden = false;
                    var propIsDefaultValueHidden = false;

                    var propDefaultValue = default(string);
                    if (prop.HasDefaultValue && prop.DefaultValue != null)
                    {
                        propDefaultValue = FormatDefaultValueForHelp(prop.DefaultValue);
                        if (propIsFlag && prop.DefaultValue is false)
                        {
                            propDefaultValue = null;
                        }
                    }
                    else if (prop.HasDefaultValue && !prop.IsArgument)
                    {
                        // Has default but we don't know the value - use placeholder to prevent [Required] tag
                        propDefaultValue = "(default)";
                        propIsDefaultValueHidden = true;
                    }

                    var propTypeName = GetShortTypeName(prop.Type.TypeSymbol);
                    parameterDefinitions.Add(new CommandOptionHelpDefinition(
                        propOptions.ToArray(),
                        propDescription,
                        propTypeName,
                        propDefaultValue,
                        propIndex,
                        propIsFlag,
                        propIsParams,
                        propIsHidden,
                        propIsDefaultValueHidden));
                }
                continue;
            }

            // ignore DI params.
            if (!item.IsParsable) continue;

            // -i, -input | [default=foo]...

            var index = item.ArgumentIndex == -1 ? null : (int?)item.ArgumentIndex;
            var options = new List<string>();
            if (item.ArgumentIndex != -1)
            {
                options.Add($"[{item.ArgumentIndex}]");
            }
            else
            {
                // aliases first
                foreach (var alias in item.Aliases)
                {
                    options.Add(alias);
                }
                if (item.Name != null)
                {
                    options.Add("--" + item.Name);
                }
            }

            var description = item.Description;
            var isFlag = item.Type.SpecialType == SpecialType.System_Boolean;
            var isParams = item.IsParams;
            var isHidden = item.IsHidden;
            var isDefaultValueHidden = item.IsDefaultValueHidden;

            var defaultValue = default(string);
            if (item.HasDefaultValue)
            {
                defaultValue = item.DefaultValue == null ? "null" : item.DefaultValueToString(castValue: false, enumIncludeTypeName: false);
                if (isFlag)
                {
                    if (item.DefaultValue is true)
                    {
                        // bool option with true default value is not flag.
                        isFlag = false;
                    }
                    else if (item.DefaultValue is false)
                    {
                        // false default value should be omitted for flag.
                        defaultValue = null;
                    }
                }
            }

            var paramTypeName = item.ToTypeShortString();
            parameterDefinitions.Add(new CommandOptionHelpDefinition(options.Distinct().ToArray(), description, paramTypeName, defaultValue, index, isFlag, isParams, isHidden, isDefaultValueHidden));
        }

        var commandName = descriptor.Name;
        return new CommandHelpDefinition(
            commandName,
            parameterDefinitions.ToArray(),
            descriptor.Description
        );
    }

    static string FormatDefaultValueForHelp(object value)
    {
        if (value is string s)
        {
            return s;
        }
        if (value is bool b)
        {
            return b ? "true" : "false";
        }
        return value.ToString() ?? "null";
    }

    static string GetShortTypeName(ITypeSymbol type)
    {
        // Handle nullable types
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            var namedType = (INamedTypeSymbol)type;
            return GetShortTypeName(namedType.TypeArguments[0]) + "?";
        }

        // Use simple names for common types
        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean: return "bool";
            case SpecialType.System_Byte: return "byte";
            case SpecialType.System_SByte: return "sbyte";
            case SpecialType.System_Int16: return "short";
            case SpecialType.System_UInt16: return "ushort";
            case SpecialType.System_Int32: return "int";
            case SpecialType.System_UInt32: return "uint";
            case SpecialType.System_Int64: return "long";
            case SpecialType.System_UInt64: return "ulong";
            case SpecialType.System_Single: return "float";
            case SpecialType.System_Double: return "double";
            case SpecialType.System_Decimal: return "decimal";
            case SpecialType.System_Char: return "char";
            case SpecialType.System_String: return "string";
            case SpecialType.System_DateTime: return "DateTime";
        }

        // For enums and other types, use the simple name
        return type.Name;
    }

    class CommandHelpDefinition
    {
        public string CommandName { get; }
        public CommandOptionHelpDefinition[] Options { get; }
        public string Description { get; }

        public CommandHelpDefinition(string command, CommandOptionHelpDefinition[] options, string description)
        {
            CommandName = command;
            Options = options;
            Description = description;
        }

        public string ToCliSchema()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"new CommandHelpDefinition(");
            sb.AppendLine($"    \"{EscapeString(CommandName)}\",");
            sb.AppendLine($"    new CommandOptionHelpDefinition[]");
            sb.AppendLine($"    {{");

            for (int i = 0; i < Options.Length; i++)
            {
                sb.Append("        ");
                sb.Append(Options[i].ToCliSchema());
                if (i < Options.Length - 1)
                {
                    sb.AppendLine(",");
                }
                else
                {
                    sb.AppendLine();
                }
            }

            sb.AppendLine($"    }},");
            sb.AppendLine($"    \"{EscapeString(Description)}\"");
            sb.Append($")");

            return sb.ToString();
        }

        private static string EscapeString(string str)
        {
            return str.Replace("\\", "\\\\")
                      .Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r")
                      .Replace("\t", "\\t");
        }
    }

    class CommandOptionHelpDefinition
    {
        public string[] Options { get; }
        public string Description { get; }
        public string? DefaultValue { get; }
        public string ValueTypeName { get; }
        public int? Index { get; }
        public bool IsRequired => DefaultValue == null && !IsParams;
        public bool IsFlag { get; }
        public bool IsParams { get; }
        public bool IsHidden { get; }
        public bool IsDefaultValueHidden { get; }
        public string FormattedValueTypeName => "<" + ValueTypeName + ">";

        public CommandOptionHelpDefinition(string[] options, string description, string valueTypeName, string? defaultValue, int? index, bool isFlag, bool isParams, bool isHidden, bool isDefaultValueHidden)
        {
            Options = options;
            Description = description;
            ValueTypeName = valueTypeName;
            DefaultValue = defaultValue;
            Index = index;
            IsFlag = isFlag;
            IsParams = isParams;
            IsHidden = isHidden;
            IsDefaultValueHidden = isDefaultValueHidden;
        }

        public string ToCliSchema()
        {
            var optionsArray = string.Join(", ", Options.Select(o => $"\"{EscapeString(o)}\""));
            var defaultValueStr = DefaultValue == null ? "null" : $"\"{EscapeString(DefaultValue)}\"";
            var indexStr = Index.HasValue ? Index.Value.ToString() : "null";

            return $"new CommandOptionHelpDefinition(new[] {{ {optionsArray} }}, \"{EscapeString(Description)}\", \"{EscapeString(ValueTypeName)}\", {defaultValueStr}, {indexStr}, {IsFlag.ToString().ToLower()}, {IsParams.ToString().ToLower()}, {IsHidden.ToString().ToLower()}, {IsDefaultValueHidden.ToString().ToLower()})";
        }

        private static string EscapeString(string str)
        {
            return str.Replace("\\", "\\\\")
                      .Replace("\"", "\\\"")
                      .Replace("\n", "\\n")
                      .Replace("\r", "\\r")
                      .Replace("\t", "\\t");
        }
    }
}
