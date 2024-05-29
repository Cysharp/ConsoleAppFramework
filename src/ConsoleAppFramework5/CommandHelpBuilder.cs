using Microsoft.CodeAnalysis;
using System.Text;

namespace ConsoleAppFramework;

public class CommandHelpBuilder
{
    //public string BuildHelpMessage(CommandDescriptor? defaultCommand, IEnumerable<CommandDescriptor> commands, bool shortCommandName)
    //{
    //    var sb = new StringBuilder();

    //    bool showHeader = (defaultCommand != null);
    //    if (defaultCommand != null)
    //    {
    //        // Display a help messages for default method
    //        sb.Append(BuildHelpMessage(CreateCommandHelpDefinition(defaultCommand, shortCommandName), showCommandName: false, fromMultiCommand: false));
    //    }

    //    var orderedCommands = options.HelpSortCommandsByFullName
    //        ? commands.OrderBy(x => x.GetCommandName(options)).ToArray()
    //        : commands.OrderBy(x => x.GetNamesFormatted(options)).ToArray();
    //    if (orderedCommands.Length > 0)
    //    {
    //        if (defaultCommand == null)
    //        {
    //            sb.Append(BuildUsageMessage());
    //            sb.AppendLine();
    //        }

    //        sb.Append(BuildMethodListMessage(orderedCommands, shortCommandName, out var maxWidth));
    //    }

    //    return sb.ToString();
    //}

    public string BuildHelpMessage(Command command)
    {
        return BuildHelpMessage(CreateCommandHelpDefinition(command, false), showCommandName: false, fromMultiCommand: false);
    }

    public string BuildHelpMessage(Command[] commands)
    {
        // TODO:
        return "";
        // return BuildHelpMessage(CreateCommandHelpDefinition(command, false), showCommandName: false, fromMultiCommand: false);
    }


    internal string BuildHelpMessage(CommandHelpDefinition definition, bool showCommandName, bool fromMultiCommand)
    {
        var sb = new StringBuilder();

        sb.AppendLine(BuildUsageMessage(definition, showCommandName, fromMultiCommand));
        sb.AppendLine();

        if (!string.IsNullOrEmpty(definition.Description))
        {
            sb.AppendLine(definition.Description);
            sb.AppendLine();
        }

        if (definition.Options.Any())
        {
            sb.Append(BuildArgumentsMessage(definition));
            sb.Append(BuildOptionsMessage(definition));
        }
        else
        {
            sb.AppendLine("Options:");
            sb.AppendLine("  ()");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    internal string BuildUsageMessage()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Usage: <Command>");

        return sb.ToString();
    }

    internal string BuildUsageMessage(CommandHelpDefinition definition, bool showCommandName, bool fromMultiCommand)
    {
        var sb = new StringBuilder();
        sb.Append($"Usage:");

        if (showCommandName)
        {
            sb.Append($" {definition.Command}");
        }

        foreach (var opt in definition.Options.Where(x => x.Index.HasValue))
        {
            sb.Append($" <{(string.IsNullOrEmpty(opt.Description) ? opt.Options[0] : opt.Description)}>");
        }

        if (definition.Options.Any(x => !x.Index.HasValue))
        {
            sb.Append(" [options...]");
        }

        return sb.ToString();
    }

    internal string BuildArgumentsMessage(CommandHelpDefinition definition)
    {
        var argumentsFormatted = definition.Options
            .Where(x => x.Index.HasValue)
            .Select(x => (Argument: $"[{x.Index}] {x.FormattedValueTypeName}", x.Description))
            .ToArray();

        if (!argumentsFormatted.Any()) return string.Empty;

        var maxWidth = argumentsFormatted.Max(x => x.Argument.Length);

        var sb = new StringBuilder();

        sb.AppendLine("Arguments:");
        foreach (var arg in argumentsFormatted)
        {
            var padding = maxWidth - arg.Argument.Length;

            sb.Append("  ");
            sb.Append(arg.Argument);
            for (var i = 0; i < padding; i++)
            {
                sb.Append(' ');
            }

            sb.Append("    ");
            sb.AppendLine(arg.Description);
        }

        sb.AppendLine();

        return sb.ToString();
    }

    internal string BuildOptionsMessage(CommandHelpDefinition definition)
    {
        var optionsFormatted = definition.Options
            .Where(x => !x.Index.HasValue)
            .Select(x => (Options: string.Join(", ", x.Options) + (x.IsFlag ? string.Empty : $" {x.FormattedValueTypeName}"), x.Description, x.IsRequired, x.IsFlag, x.DefaultValue))
            .ToArray();

        if (!optionsFormatted.Any()) return string.Empty;

        var maxWidth = optionsFormatted.Max(x => x.Options.Length);

        var sb = new StringBuilder();

        sb.AppendLine("Options:");
        foreach (var opt in optionsFormatted)
        {
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

            sb.AppendLine();
        }

        sb.AppendLine();

        return sb.ToString();
    }

    //internal string BuildMethodListMessage(IEnumerable<CommandDescriptor> types, bool shortCommandName, out int maxWidth)
    //{
    //    maxWidth = 0;
    //    return BuildMethodListMessage(types.Select(x => CreateCommandHelpDefinition(x, shortCommandName)), true, out maxWidth);
    //}

    //internal string BuildMethodListMessage(IEnumerable<CommandHelpDefinition> commandHelpDefinitions, bool appendCommand, out int maxWidth)
    //{
    //    var formatted = commandHelpDefinitions
    //        .Select(x => (Command: $"{(x.CommandAliases.Length != 0 ? ((appendCommand ? x.Command + " " : "") + string.Join(", ", x.CommandAliases)) : x.Command)}", Description: x.Description))
    //        .ToArray();
    //    maxWidth = formatted.Max(x => x.Command.Length);

    //    var sb = new StringBuilder();

    //    sb.AppendLine("Commands:");
    //    foreach (var item in formatted)
    //    {
    //        sb.Append("  ");
    //        sb.Append(item.Command);

    //        var padding = maxWidth - item.Command.Length;
    //        for (var i = 0; i < padding; i++)
    //        {
    //            sb.Append(' ');
    //        }

    //        sb.Append("    ");
    //        sb.AppendLine(item.Description);
    //    }

    //    return sb.ToString();
    //}

    internal CommandHelpDefinition CreateCommandHelpDefinition(Command descriptor, bool shortCommandName)
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
                options.Add("--" + item.Name);
                foreach (var alias in item.Aliases)
                {
                    options.Add(alias);
                }
            }

            var description = item.Description;
            var isFlag = item.Type.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Boolean;

            var defaultValue = default(string);
            if (item.HasDefaultValue)
            {
                defaultValue = (item.DefaultValue?.ToString() ?? "null");
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

            var paramTypeName = item.ToTypeDisplayString();
            if (item.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                paramTypeName = ((item.Type as INamedTypeSymbol)!.TypeArguments[0]).ToFullyQualifiedFormatDisplayString() + "?";
            }

            parameterDefinitions.Add(new CommandOptionHelpDefinition(options.Distinct().ToArray(), description, paramTypeName, defaultValue, index, isFlag));
        }

        return new CommandHelpDefinition(
            descriptor.CommandName,
            // descriptor.Aliases,
            parameterDefinitions.ToArray(),
            descriptor.Description
        );
    }

    public class CommandHelpDefinition
    {
        // TODO: Command Path

        public string Command { get; }
        public CommandOptionHelpDefinition[] Options { get; }
        public string Description { get; }

        public CommandHelpDefinition(string command, CommandOptionHelpDefinition[] options, string description)
        {
            Command = command;
            Options = options;
            Description = description;
        }
    }

    // TODO: params?
    public class CommandOptionHelpDefinition
    {
        public string[] Options { get; }
        public string Description { get; }
        public string? DefaultValue { get; }
        public string ValueTypeName { get; }
        public int? Index { get; }

        public bool IsRequired => DefaultValue == null;
        public bool IsFlag { get; }
        public string FormattedValueTypeName => "<" + ValueTypeName + ">";

        public CommandOptionHelpDefinition(string[] options, string description, string valueTypeName, string? defaultValue, int? index, bool isFlag)
        {
            Options = options;
            Description = description;
            ValueTypeName = valueTypeName;
            DefaultValue = defaultValue;
            Index = index;
            IsFlag = isFlag;
        }
    }
}
