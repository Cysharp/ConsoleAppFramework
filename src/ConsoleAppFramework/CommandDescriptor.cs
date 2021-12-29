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
        public string? RootCommand { get; }

        // TODO:names???
        public string Name
        {
            get
            {
                if (CommandAttribute != null) return CommandAttribute.CommandNames[0];
                // TODO: foo-bar name???
                return MethodInfo.Name.ToLower();
            }
        }

        public string[] Names
        {
            get
            {
                if (CommandAttribute != null) return CommandAttribute.CommandNames;
                // TODO: foo-bar name???
                return new[] { MethodInfo.Name.ToLower() };
            }
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

        public string CommandName
        {
            get
            {
                if (RootCommand != null)
                {
                    return $"{RootCommand} {Name}";
                }
                else
                {
                    return Name;
                }
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

        public CommandDescriptor(CommandType commandType, MethodInfo methodInfo, object? instance = null, CommandAttribute? additionalCommandAttribute = null, string? rootCommand = null)
        {
            CommandType = commandType;
            MethodInfo = methodInfo;
            Instance = instance;
            CommandAttribute = additionalCommandAttribute ?? methodInfo.GetCustomAttribute<CommandAttribute>();
            RootCommand = rootCommand;
        }
    }
}