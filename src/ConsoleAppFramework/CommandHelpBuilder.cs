using Microsoft.CodeAnalysis;
using System.Text;

namespace ConsoleAppFramework;

public static class CommandHelpBuilder
{
    public static string BuildRootHelpMessage(Command command)
    {
        return BuildHelpMessageCore(command, showCommandName: false, showCommand: false);
    }

    public static string BuildRootHelpMessage(Command[] commands)
    {
        var sb = new StringBuilder();

        var rootCommand = commands.FirstOrDefault(x => x.IsRootCommand);
        var withoutRoot = commands.Where(x => !x.IsRootCommand).ToArray();

        if (rootCommand != null && withoutRoot.Length == 0)
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

        if (withoutRoot.Length == 0) return sb.ToString();

        var helpDefinitions = withoutRoot.OrderBy(x => x.Name).ToArray();

        var list = BuildMethodListMessage(helpDefinitions, out _);
        sb.Append(list);

        return sb.ToString();
    }

    public static string BuildCommandHelpMessage(Command command)
    {
        return BuildHelpMessageCore(command, showCommandName: command.Name != "", showCommand: false);
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
            .Select(x => (Options: string.Join("|", x.Options) + (x.IsFlag ? string.Empty : $" {x.FormattedValueTypeName}{(x.IsParams ? "..." : "")}"), x.Description, x.IsRequired, x.IsFlag, x.DefaultValue))
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

            if (opt.IsFlag)
            {
                sb.Append($" (Optional)");
            }
            else if (opt.DefaultValue != null)
            {
                sb.Append($" (Default: {opt.DefaultValue})");
            }
            else if (opt.IsRequired)
            {
                sb.Append($" (Required)");
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
                return (Command: x.Name, x.Description);
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
                options.Add("--" + item.Name);
            }

            var description = item.Description;
            var isFlag = item.Type.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Boolean;
            var isParams = item.IsParams;
            var isHidden = item.IsHidden;

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
            parameterDefinitions.Add(new CommandOptionHelpDefinition(options.Distinct().ToArray(), description, paramTypeName, defaultValue, index, isFlag, isParams, isHidden));
        }

        var commandName = descriptor.Name;
        return new CommandHelpDefinition(
            commandName,
            parameterDefinitions.ToArray(),
            descriptor.Description
        );
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
        public string FormattedValueTypeName => "<" + ValueTypeName + ">";

        public CommandOptionHelpDefinition(string[] options, string description, string valueTypeName, string? defaultValue, int? index, bool isFlag, bool isParams, bool isHidden)
        {
            Options = options;
            Description = description;
            ValueTypeName = valueTypeName;
            DefaultValue = defaultValue;
            Index = index;
            IsFlag = isFlag;
            IsParams = isParams;
            IsHidden = isHidden;
        }
    }
}
