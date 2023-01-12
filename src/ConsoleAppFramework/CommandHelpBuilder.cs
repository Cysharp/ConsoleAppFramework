using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ConsoleAppFramework
{
    internal class CommandHelpBuilder
    {
        readonly Func<string> getExecutionCommandName;
        readonly Func<string[]> getExecutionCommandAliases;
        readonly bool isStrictOption;
        readonly IServiceProviderIsService isService;
        readonly ConsoleAppOptions options;

        public CommandHelpBuilder(Func<string>? getExecutionCommandName, Func<string[]>? getExecutionCommandAliases, IServiceProviderIsService isService, ConsoleAppOptions options)
        {
            this.getExecutionCommandName = getExecutionCommandName ?? GetExecutionCommandNameDefault;
            this.getExecutionCommandAliases = getExecutionCommandAliases ?? (() => new string[0]);
            this.isStrictOption = options.StrictOption;
            this.isService = isService;
            this.options = options;
        }

        private string GetExecutionCommandNameDefault()
        {
            if (options.ApplicationName != null)
            {
                return options.ApplicationName;
            }
            else
            {
                return Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()!.Location);
            }
        }

        public string GetExecutionCommandName() => getExecutionCommandName();

        public string BuildHelpMessage(CommandDescriptor? defaultCommand, IEnumerable<CommandDescriptor> commands, bool shortCommandName)
        {
            var sb = new StringBuilder();

            bool showHeader = (defaultCommand != null);
            if (defaultCommand != null)
            {
                // Display a help messages for default method
                sb.Append(BuildHelpMessage(CreateCommandHelpDefinition(defaultCommand, shortCommandName), showCommandName: false, fromMultiCommand: false));
            }

            var orderedCommands = options.HelpSortCommandsByFullName
                ? commands.OrderBy(x => x.GetCommandName(options)).ToArray()
                : commands.OrderBy(x => x.GetNamesFormatted(options)).ToArray();
            if (orderedCommands.Length > 0)
            {
                if (defaultCommand == null)
                {
                    sb.Append(BuildUsageMessage());
                    sb.AppendLine();
                }

                sb.Append(BuildMethodListMessage(orderedCommands, shortCommandName, out var maxWidth));
            }

            return sb.ToString();
        }

        public string BuildHelpMessage(CommandDescriptor command)
        {
            return BuildHelpMessage(CreateCommandHelpDefinition(command, false), showCommandName: false, fromMultiCommand: false);
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

            var aliases = getExecutionCommandAliases();
            if(aliases.Length > 0)
            {
                sb.Append("Aliases: ");
                sb.AppendLine(string.Join(", ", aliases));
            }

            return sb.ToString();
        }

        internal string BuildUsageMessage()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Usage: {GetExecutionCommandName()} <Command>");

            return sb.ToString();
        }

        internal string BuildUsageMessage(CommandHelpDefinition definition, bool showCommandName, bool fromMultiCommand)
        {
            var sb = new StringBuilder();
            sb.Append($"Usage: {GetExecutionCommandName()}");

            if (showCommandName)
            {
                sb.Append($" {(definition.CommandAliases.Any() ? ((fromMultiCommand ? definition.Command + " " : "") + definition.CommandAliases[0]) : definition.Command)}");
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

        internal string BuildMethodListMessage(IEnumerable<CommandDescriptor> types, bool shortCommandName, out int maxWidth)
        {
            maxWidth = 0;
            return BuildMethodListMessage(types.Select(x => CreateCommandHelpDefinition(x, shortCommandName)), true, out maxWidth);
        }

        internal string BuildMethodListMessage(IEnumerable<CommandHelpDefinition> commandHelpDefinitions, bool appendCommand, out int maxWidth)
        {
            var formatted = commandHelpDefinitions
                .Select(x => (Command: x.Command, Description: x.Description))
                .ToArray();
            maxWidth = formatted.Max(x => x.Command.Length);

            var sb = new StringBuilder();

            sb.AppendLine("Commands:");
            foreach (var item in formatted)
            {
                sb.Append("  ");
                sb.Append(item.Command);

                var padding = maxWidth - item.Command.Length;
                for (var i = 0; i < padding; i++)
                {
                    sb.Append(' ');
                }

                sb.Append("    ");
                sb.AppendLine(item.Description);
            }

            return sb.ToString();
        }

        internal CommandHelpDefinition CreateCommandHelpDefinition(CommandDescriptor descriptor, bool shortCommandName)
        {
            var parameterDefinitions = new List<CommandOptionHelpDefinition>();

            foreach (var item in descriptor.MethodInfo.GetParameters())
            {
                // ignore DI params.
                if (item.ParameterType == typeof(ConsoleAppContext) || (isService != null && isService.IsService(item.ParameterType))) continue;

                // -i, -input | [default=foo]...

                var index = default(int?);
                var itemName = this.options.NameConverter(item.Name!);

                var options = new List<string>();
                var option = item.GetCustomAttribute<OptionAttribute>();
                if (option != null)
                {
                    if (option.Index != -1)
                    {
                        index = option.Index;
                        options.Add($"[{option.Index}]");
                    }
                    else
                    {
                        // If Index is -1, ShortName is initialized at Constractor.
                        if (option.ShortName != null)
                        {
                            options.Add($"-{option.ShortName.Trim('-')}");
                        }
                    }
                }

                if (!index.HasValue)
                {
                    if (isStrictOption)
                    {
                        options.Add($"--{itemName}");
                    }
                    else
                    {
                        options.Add($"-{itemName}");
                    }
                }

                var description = string.Empty;
                if (option != null && !string.IsNullOrEmpty(option.Description))
                {
                    description = option.Description ?? string.Empty;
                }
                else
                {
                    description = string.Empty;
                }

                var isFlag = item.ParameterType == typeof(bool);

                var defaultValue = default(string);
                if (item.HasDefaultValue)
                {
                    if (option?.DefaultValue != null)
                    {
                        defaultValue = option.DefaultValue;
                    }
                    else
                    {
                        defaultValue = (item.DefaultValue?.ToString() ?? "null");
                    }
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

                var paramTypeName = item.ParameterType.Name;
                if (item.ParameterType.IsGenericType && item.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    paramTypeName = item.ParameterType.GetGenericArguments()[0].Name + "?";
                }

                parameterDefinitions.Add(new CommandOptionHelpDefinition(options.Distinct().ToArray(), description, paramTypeName, defaultValue, index, isFlag));
            }

            return new CommandHelpDefinition(
                shortCommandName ? descriptor.GetNames(options)[0] : descriptor.GetCommandName(options),
                descriptor.Aliases,
                parameterDefinitions.OrderBy(x => x.Index ?? int.MaxValue).ToArray(),
                descriptor.Description
            );
        }

        public class CommandHelpDefinition
        {
            public string Command { get; }
            public string[] CommandAliases { get; }
            public CommandOptionHelpDefinition[] Options { get; }
            public string Description { get; }

            public CommandHelpDefinition(string command, string[] commandAliases, CommandOptionHelpDefinition[] options, string description)
            {
                Command = command;
                CommandAliases = commandAliases;
                Options = options;
                Description = description;
            }
        }

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
}
