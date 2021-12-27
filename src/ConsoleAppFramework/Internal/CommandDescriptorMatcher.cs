using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ConsoleAppFramework
{
    internal class CommandDescriptorMatcher
    {
        CommandDescriptor? defaultCommandDescriptor;
        readonly Dictionary<string, CommandDescriptor> descriptors = new Dictionary<string, CommandDescriptor>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<(string, string), CommandDescriptor> subCommandDescriptors = new Dictionary<(string, string), CommandDescriptor>(new TupleStringComparer());

        public void AddCommand(CommandDescriptor commandDescriptor)
        {
            if (!descriptors.TryAdd(commandDescriptor.Name, commandDescriptor))
            {
                throw new InvalidOperationException($"Duplicate command name is added. Name:{commandDescriptor.Name} Method:{commandDescriptor.MethodInfo.DeclaringType?.Name}.{commandDescriptor.MethodInfo.Name}");
            }
        }

        public void AddSubCommand(string rootCommand, CommandDescriptor commandDescriptor)
        {
            if (!subCommandDescriptors.TryAdd((rootCommand, commandDescriptor.Name), commandDescriptor))
            {
                throw new InvalidOperationException($"Duplicate command name is added. Name:{rootCommand} {commandDescriptor.Name} Method:{commandDescriptor.MethodInfo.DeclaringType?.Name}.{commandDescriptor.MethodInfo.Name}");
            }
        }

        public void SetDefaultCommand(CommandDescriptor commandDescriptor)
        {
            this.defaultCommandDescriptor = commandDescriptor;
        }

        // Only check command name(not foo)
        public bool TryGetDescriptor(string[] args, [MaybeNullWhen(false)] out CommandDescriptor descriptor, out int skip)
        {
            // 1. Try to match sub command
            if (args.Length >= 2)
            {
                if (subCommandDescriptors.TryGetValue((args[0], args[1]), out descriptor))
                {
                    skip = 2;
                    return true;
                }
            }

            // 2. Try to match command
            if (args.Length >= 1)
            {
                if (descriptors.TryGetValue(args[0], out descriptor))
                {
                    skip = 1;
                    return true;
                }
            }

            // 3. default
            if (defaultCommandDescriptor != null)
            {
                skip = 0;
                descriptor = defaultCommandDescriptor;
                return true;
            }

            // not match.
            skip = 0;
            descriptor = default;
            return false;
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

#if NETSTANDARD2_0

    internal static class DictionaryHelper
    {
        internal static bool TryAdd<TKey>(this Dictionary<TKey, CommandDescriptor> dict, TKey key,  CommandDescriptor value)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, value);
                return true;
            }
            return false;
        }
    }

#endif
}