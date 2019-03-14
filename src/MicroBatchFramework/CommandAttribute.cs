using System;

namespace MicroBatchFramework
{
    public class CommandAttribute : Attribute
    {
        public string[] CommandNames { get; }
        public string Description { get; }

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

        public CommandAttribute(string[] commandNames, string description)
        {
            foreach (var item in commandNames)
            {
                if (item.Equals("list", StringComparison.OrdinalIgnoreCase)
                 || item.Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("`list` or `help` is system reserved commandName, can not use.");
                }
            }

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
