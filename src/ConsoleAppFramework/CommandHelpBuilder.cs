using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ConsoleAppFramework
{
    internal class CommandHelpBuilder
    {
        readonly Func<string> _getExecutionCommandName;

        public CommandHelpBuilder(Func<string>? getExecutionCommandName = null)
        {
            _getExecutionCommandName = getExecutionCommandName ?? GetExecutionCommandNameDefault;
        }

        private static string GetExecutionCommandNameDefault()
        {
            return Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
        }

        public string GetExecutionCommandName()
            => _getExecutionCommandName();

        public string BuildHelpMessage(IEnumerable<Type> types)
        {
            var sb = new StringBuilder();
            sb.Append(BuildUsageMessage());
            sb.AppendLine();

            sb.Append(BuildMethodListMessage(types));

            return sb.ToString();
        }

        public string BuildHelpMessage(MethodInfo[] methodInfo, MethodInfo? defaultMethod)
        {
            var sb = new StringBuilder();

            if (defaultMethod != null)
            {
                // Display a help messages for default method
                sb.Append(BuildHelpMessage(CreateCommandHelpDefinition(defaultMethod), showCommandName:false));
            }
            
            if (methodInfo.Length > 1)
            {
                // Display sub commands list.
                sb.Append(BuildUsageMessage());
                sb.AppendLine();
                sb.Append(BuildMethodListMessage(methodInfo.Where(x => x != defaultMethod).Select(x => CreateCommandHelpDefinition(x))));
            }

            sb.AppendLine();

            return sb.ToString();
        }

        public string BuildHelpMessage(MethodInfo methodInfo, bool showCommandName)
            => BuildHelpMessage(CreateCommandHelpDefinition(methodInfo), showCommandName);

        public string BuildHelpMessage(CommandHelpDefinition definition, bool showCommandName)
        {
            var sb = new StringBuilder();

            sb.AppendLine(BuildUsageMessage(definition, showCommandName));
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

            return sb.ToString();
        }

        public string BuildUsageMessage()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Usage: {GetExecutionCommandName()} <Command>");

            return sb.ToString();
        }

        public string BuildUsageMessage(CommandHelpDefinition definition, bool showCommandName)
        {
            var sb = new StringBuilder();
            sb.Append($"Usage: {GetExecutionCommandName()}");

            if (showCommandName)
            {
                sb.Append($" {(definition.CommandAliases.Any() ? definition.CommandAliases[0] : definition.Command)}");
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
                .Select(x => (Argument: $"[{x.Index}] <{x.ValueTypeName}>", x.Description, x.IsRequired, x.DefaultValue))
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
                .Select(x => (Options: string.Join(", ", x.Options) + $" <{x.ValueTypeName}>", x.Description, x.IsRequired, x.DefaultValue))
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

                if (opt.DefaultValue != null)
                {
                    sb.Append($" (Default: {opt.DefaultValue})");
                }
                if (opt.IsRequired)
                {
                    sb.Append($" (Required)");
                }

                sb.AppendLine();
            }

            sb.AppendLine();

            return sb.ToString();
        }

        public string BuildMethodListMessage(IEnumerable<Type> types)
        {
            return BuildMethodListMessage(types
                .SelectMany(xs => xs.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Select(x => CreateCommandHelpDefinition(x))));
        }

        public string BuildMethodListMessage(IEnumerable<CommandHelpDefinition> commandHelpDefinitions)
        {
            var formatted = commandHelpDefinitions
                .Select(x => (Command: $"{(x.CommandAliases.Length != 0 ? string.Join(", ", x.CommandAliases) : x.Command)}", Description: x.Description))
                .ToArray();
            var maxWidth = formatted.Max(x => x.Command.Length);

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

            sb.AppendLine();

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
                        options.Add($"-{option.ShortName!.Trim('-')}");
                    }
                }

                if (!index.HasValue)
                {
                    options.Add($"-{item.Name}");
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

                var defaultValue = default(string);
                if (item.HasDefaultValue)
                {
                    defaultValue = (item.DefaultValue?.ToString() ?? "null");
                }

                parameterDefinitions.Add(new CommandOptionHelpDefinition(options.Distinct().ToArray(), description, item.ParameterType.Name, defaultValue, index));
            }

            return new CommandHelpDefinition(
                $"{method.DeclaringType.Name}.{method.Name}",
                command?.CommandNames ?? Array.Empty<string>(),
                parameterDefinitions.OrderBy(x => x.Index ?? int.MaxValue).ToArray(),
                command?.Description ?? String.Empty
            );
        }

        class CustomSorter : IComparer<MethodInfo>
        {
            public int Compare(MethodInfo x, MethodInfo y)
            {
                if (x.Name == y.Name)
                {
                    return 0;
                }

                var xc = x.GetCustomAttribute<CommandAttribute>();
                var yc = y.GetCustomAttribute<CommandAttribute>();

                if (xc != null)
                {
                    return 1;
                }
                if (yc != null)
                {
                    return -1;
                }

                return x.Name.CompareTo(y.Name);
            }
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

            public CommandOptionHelpDefinition(string[] options, string description, string valueTypeName, string? defaultValue, int? index)
            {
                Options = options;
                Description = description;
                ValueTypeName = valueTypeName;
                DefaultValue = defaultValue;
                Index = index;
            }
        }

    }
}
