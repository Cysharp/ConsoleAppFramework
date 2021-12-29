using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ConsoleAppFramework
{
    internal class CommandDescriptorCollection
    {
        CommandDescriptor? defaultCommandDescriptor;
        readonly Dictionary<string, CommandDescriptor> descriptors = new Dictionary<string, CommandDescriptor>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Dictionary<string, CommandDescriptor>> subCommandDescriptors = new Dictionary<string, Dictionary<string, CommandDescriptor>>(StringComparer.OrdinalIgnoreCase);
        readonly ConsoleAppOptions options;

        public CommandDescriptorCollection(ConsoleAppOptions options)
        {
            this.options = options;
        }

        public void AddCommand(CommandDescriptor commandDescriptor)
        {
            foreach (var name in commandDescriptor.GetNames(options))
            {
                if (subCommandDescriptors.ContainsKey(name) || !descriptors.TryAdd(name, commandDescriptor))
                {
                    throw new InvalidOperationException($"Duplicate command name is added. Name:{name} Method:{commandDescriptor.MethodInfo.DeclaringType?.Name}.{commandDescriptor.MethodInfo.Name}");
                }
            }
        }

        public void AddSubCommand(string rootCommand, CommandDescriptor commandDescriptor)
        {
            if (descriptors.ContainsKey(rootCommand))
            {
                throw new InvalidOperationException($"Duplicate root-command is added. Name:{rootCommand} Method:{commandDescriptor.MethodInfo.DeclaringType?.Name}.{commandDescriptor.MethodInfo.Name}");
            }

            if (!subCommandDescriptors.TryGetValue(rootCommand, out var commandDict))
            {
                commandDict = new Dictionary<string, CommandDescriptor>(StringComparer.OrdinalIgnoreCase);
                subCommandDescriptors.Add(rootCommand, commandDict);
            }

            foreach (var name in commandDescriptor.GetNames(options))
            {
                if (!commandDict.TryAdd(name, commandDescriptor))
                {
                    throw new InvalidOperationException($"Duplicate command name is added. Name:{rootCommand} {name} Method:{commandDescriptor.MethodInfo.DeclaringType?.Name}.{commandDescriptor.MethodInfo.Name}");
                }
            }
        }

        public void AddDefaultCommand(CommandDescriptor commandDescriptor)
        {
            if (this.defaultCommandDescriptor != null)
            {
                throw new InvalidOperationException($"Found more than one default command. Method:{defaultCommandDescriptor.MethodInfo.DeclaringType?.Name}.{defaultCommandDescriptor.MethodInfo.Name} and {commandDescriptor.MethodInfo.DeclaringType?.Name}.{commandDescriptor.MethodInfo.Name}");
            }

            this.defaultCommandDescriptor = commandDescriptor;
        }

        // Only check command name(not foo)
        public bool TryGetDescriptor(string[] args, [MaybeNullWhen(false)] out CommandDescriptor descriptor, out int offset)
        {
            // 1. Try to match sub command
            if (args.Length >= 2)
            {
                if (subCommandDescriptors.TryGetValue(args[0], out var dict))
                {
                    if (dict.TryGetValue(args[1], out descriptor))
                    {
                        offset = 2;
                        return true;
                    }
                    else
                    {
                        goto NOTMATCH;
                    }
                }
            }

            // 2. Try to match command
            if (args.Length >= 1)
            {
                if (descriptors.TryGetValue(args[0], out descriptor))
                {
                    offset = 1;
                    return true;
                }
            }

            // 3. default
            if (defaultCommandDescriptor != null)
            {
                offset = 0;
                descriptor = defaultCommandDescriptor;
                return true;
            }

        // not match.
        NOTMATCH:
            offset = 0;
            descriptor = default;
            return false;
        }

        public void TryAddDefaultHelpMethod()
        {
            // add if not exists.
            descriptors.TryAdd(DefaultCommands.Help, DefaultCommands.HelpCommand);
        }

        public void TryAddDefaultVersionMethod()
        {
            descriptors.TryAdd(DefaultCommands.Version, DefaultCommands.VersionCommand);
        }

        public bool TryGetHelpMethod([MaybeNullWhen(false)] out CommandDescriptor commandDescriptor)
        {
            return descriptors.TryGetValue(DefaultCommands.Help, out commandDescriptor);
        }

        public bool TryGetVersionMethod([MaybeNullWhen(false)] out CommandDescriptor commandDescriptor)
        {
            return descriptors.TryGetValue(DefaultCommands.Version, out commandDescriptor);
        }

        public CommandDescriptor? GetDefaultCommandDescriptor() => defaultCommandDescriptor;

        /// <summary>
        /// GetAll(except default) descriptors.
        /// </summary>
        public IEnumerable<CommandDescriptor> GetAllDescriptors()
        {
            IEnumerable<CommandDescriptor> IterateCore()
            {
                foreach (var item in descriptors.Values)
                {
                    yield return item;
                }
                foreach (var item in subCommandDescriptors.Values)
                {
                    foreach (var item2 in item.Values)
                    {
                        yield return item2;
                    }
                }
            }

            return IterateCore().Distinct();
        }

        public CommandDescriptor[] GetSubCommands(string rootCommand)
        {
            if (subCommandDescriptors.TryGetValue(rootCommand, out var dict))
            {
                return dict.Values.Distinct().ToArray();
            }

            return Array.Empty<CommandDescriptor>();
        }

        class TupleStringComparer : IEqualityComparer<(string, string)>
        {
            public bool Equals((string, string) x, (string, string) y)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(x.Item1, y.Item1))
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(x.Item2, y.Item2))
                    {
                        return true;
                    }
                }
                return false;
            }

            public int GetHashCode((string, string) obj)
            {
                return (StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1), StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2)).GetHashCode();
            }
        }
    }
}