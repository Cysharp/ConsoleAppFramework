using System.Reflection;

namespace ConsoleAppFramework
{
    internal class CommandDescriptor
    {
        public MethodInfo MethodInfo { get; }
        public object? Instance { get; }
        public CommandAttribute? CommandAttribute { get; }

        public string Name
        {
            get
            {
                if (CommandAttribute != null) return CommandAttribute.CommandNames[0];
                // TODO: foo-bar name???
                return MethodInfo.Name;
            }
        }

        public CommandDescriptor(MethodInfo methodInfo, object? instance, CommandAttribute? commandAttribute)
        {
            MethodInfo = methodInfo;
            Instance = instance;
            CommandAttribute = commandAttribute;
        }
    }
}