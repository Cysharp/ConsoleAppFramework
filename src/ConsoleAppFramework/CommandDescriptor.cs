using System;
using System.Linq;
using System.Reflection;

namespace ConsoleAppFramework
{
    internal enum CommandType
    {
        DefaultCommand,
        Command,
        SubCommand
    }

    internal class CommandDescriptor
    {
        public CommandType CommandType { get; }
        public MethodInfo MethodInfo { get; }
        public object? Instance { get; }
        public CommandAttribute? CommandAttribute { get; }
        public string? ParentCommand { get; }

        public string[] GetNames(ConsoleAppOptions options)
        {
            if (CommandAttribute != null) return CommandAttribute.CommandNames;
            return new[] { options.NameConverter(MethodInfo.Name) };
        }

        public string GetNamesFormatted(ConsoleAppOptions options)
        {
            return string.Join(", ", GetNames(options));
        }

        public string[] Aliases
        {
            get
            {
                if (CommandAttribute == null || CommandAttribute.CommandNames.Length <= 1)
                {
                    return Array.Empty<string>();
                }
                else
                {
                    return CommandAttribute.CommandNames.Skip(1).ToArray();
                }
            }
        }

        public string GetCommandName(ConsoleAppOptions options)
        {
            if (ParentCommand != null)
            {
                return $"{ParentCommand} {GetNames(options)[0]}";
            }
            else
            {
                return GetNames(options)[0];
            }
        }

        public string Description
        {
            get
            {
                if (CommandAttribute != null)
                {
                    return CommandAttribute.Description ?? "";
                }
                else
                {
                    return "";
                }
            }
        }

        public CommandDescriptor(CommandType commandType, MethodInfo methodInfo, object? instance = null, CommandAttribute? additionalCommandAttribute = null, string? parentCommand = null)
        {
            CommandType = commandType;
            MethodInfo = methodInfo;
            Instance = instance;
            CommandAttribute = additionalCommandAttribute ?? methodInfo.GetCustomAttribute<CommandAttribute>();
            ParentCommand = parentCommand;
        }
    }
}