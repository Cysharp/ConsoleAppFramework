using System;

namespace MicroBatchFramework
{
    public class CommandAttribute : Attribute
    {
        public string CommandName { get; }
        public string Description { get; }

        public CommandAttribute(string commandName)
            : this(commandName, null)
        {
        }

        public CommandAttribute(string commandName, string description)
        {
            if (commandName.Equals("list", StringComparison.OrdinalIgnoreCase)
             || commandName.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("`list` or `help` is system reserved commandName, can not use.");
            }

            this.CommandName = commandName;
            this.Description = description;
        }
    }
}
