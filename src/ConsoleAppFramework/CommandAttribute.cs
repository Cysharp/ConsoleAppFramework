using System;

namespace ConsoleAppFramework
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        public string[] CommandNames { get; }
        public string? Description { get; }

        public CommandAttribute(string commandName)
            : this(new[] { commandName }, null)
        {
        }

        public CommandAttribute(string commandName, string description)
            : this(new[] { commandName }, description)
        {
        }

        public CommandAttribute(string[] commandNames)
            : this(commandNames, null)
        {
        }

        public CommandAttribute(string[] commandNames, string? description)
        {
            this.CommandNames = commandNames;
            this.Description = description;
        }

        internal bool EqualsAny(string name)
        {
            foreach (var item in CommandNames)
            {
                if (string.Equals(name, item, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
