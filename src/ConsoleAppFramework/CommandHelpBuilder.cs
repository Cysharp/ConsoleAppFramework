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
        readonly Func<string> _getExecutionCommandName;
        readonly bool _isStrictOption;
        readonly bool _isShowDefaultOption;

        public CommandHelpBuilder(Func<string>? getExecutionCommandName, bool isStrictOption, bool isShowDefaultOption)
        {
            _getExecutionCommandName = getExecutionCommandName ?? GetExecutionCommandNameDefault;
            _isStrictOption = isStrictOption;
            _isShowDefaultOption = isShowDefaultOption;
        }

        private static string GetExecutionCommandNameDefault()
        {
            return Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly()!.Location);
        }

        public string GetExecutionCommandName()
            => _getExecutionCommandName();

        public string BuildHelpMessage(IEnumerable<Type> types)
        {
            var sb = new StringBuilder();
            sb.Append(BuildUsageMessage());
            sb.AppendLine();

            sb.Append(BuildMethodListMessage(types, out var maxWidth));
            if (_isShowDefaultOption)
            {
                BuildDefaultHelp(sb, maxWidth, false);
            }

            return sb.ToString();
        }

        public string BuildHelpMessage(MethodInfo[] methodInfo, MethodInfo? defaultMethod)
        {
            var sb = new StringBuilder();

            bool showHeader = (defaultMethod != null);
            if (defaultMethod != null)
            {
                // Display a help messages for default method
                sb.Append(BuildHelpMessage(CreateCommandHelpDefinition(defaultMethod), showCommandName: false, fromMultiCommand: false));
            }

            int maxWidth = 10;
            if ((defaultMethod == null && methodInfo.Length == 1) || methodInfo.Length > 1)
            {
                // Display sub commands list.
                sb.Append(BuildUsageMessage());
                sb.AppendLine();

                var list = methodInfo.Where(x => x != defaultMethod).Select(x => CreateCommandHelpDefinition(x)).ToArray();
                sb.Append(BuildMethodListMessage(list, false, out maxWidth));
                showHeader = false;
            }

            if (_isShowDefaultOption)
            {
                BuildDefaultHelp(sb, maxWidth, showHeader);
            }

            return sb.ToString();
        }

        static void BuildDefaultHelp(StringBuilder sb, int maxWidth, bool showHeader)
        {
            if (showHeader)
            {
                sb.AppendLine("Commands:");
            }

            var padding = Math.Max(0, maxWidth - "help".Length);
            sb.AppendLine("  help" + new string(' ', padding) + "    Display help.");
            padding = Math.Max(0, maxWidth - "version".Length);
            sb.AppendLine("  version" + new string(' ', padding) + "    Display version.");
        }


        public string BuildHelpMessage(MethodInfo methodInfo, bool showCommandName, bool fromMultiCommand)
            => BuildHelpMessage(CreateCommandHelpDefinition(methodInfo), showCommandName, fromMultiCommand);

        public string BuildHelpMessage(CommandHelpDefinition definition, bool showCommandName, bool fromMultiCommand)
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

        public string BuildUsageMessage()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Usage: {GetExecutionCommandName()} <Command>");

            return sb.ToString();
        }

        public string BuildUsageMessage(CommandHelpDefinition definition, bool showCommandName, bool fromMultiCommand)
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

        public string BuildArgumentsMessage(CommandHelpDefinition definition)
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

        public string BuildOptionsMessage(CommandHelpDefinition definition)
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

        public string BuildMethodListMessage(IEnumerable<Type> types, out int maxWidth)
        {
            return BuildMethodListMessage(types
                .SelectMany(xs => xs.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(x => CreateCommandHelpDefinition(x))), true, out maxWidth);
        }

        public string BuildMethodListMessage(IEnumerable<CommandHelpDefinition> commandHelpDefinitions, bool appendCommand, out int maxWidth)
        {
            var formatted = commandHelpDefinitions
                .Select(x => (Command: $"{(x.CommandAliases.Length != 0 ? ((appendCommand ? x.Command + " " : "") + string.Join(", ", x.CommandAliases)) : x.Command)}", Description: x.Description))
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

            // sb.AppendLine();

            return sb.ToString();
        }

        public CommandHelpDefinition CreateCommandHelpDefinition(MethodInfo method)
        {
            var command = method.GetCustomAttribute<CommandAttribute>();

            var parameterDefinitions = new List<CommandOptionHelpDefinition>();

            foreach (var item in method.GetParameters())
            {
                // -i, -input | [default=foo]...

                var index = default(int?);
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
                    if (_isStrictOption)
                    {
                        options.Add($"--{item.Name}");
                    }
                    else
                    {
                        options.Add($"-{item.Name}");
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
                method.DeclaringType.GetCustomAttribute<CommandAttribute>()?.CommandNames?.FirstOrDefault() ?? method.DeclaringType!.Name.ToLower(),
                command?.CommandNames ?? new[] { method.Name.ToLower() },
                parameterDefinitions.OrderBy(x => x.Index ?? int.MaxValue).ToArray(),
                command?.Description ?? String.Empty
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
