using System.Text.Json;

namespace ConsoleAppFramework
{
    public class ConsoleAppOptions
    {
        /// <summary>
        /// Argument parser uses strict(-short, --long) option. Default is true.
        /// </summary>
        public bool StrictOption { get; set; } = true;

        /// <summary>
        /// Show default command(help/version) to help. Default is true.
        /// </summary>
        public bool ShowDefaultCommand { get; set; } = true;

        public JsonSerializerOptions? JsonSerializerOptions { get; set; }

        public ConsoleAppFilter[]? GlobalFilters { get; set; }

        public bool NoAttributeCommandAsImplicitlyDefault { get; set; }

        // internal store values for execute engine.

        internal string[] CommandLineArguments { get; set; } = default!;
        internal CommandDescriptorCollection CommandDescriptors { get; } = new CommandDescriptorCollection();

        public static ConsoleAppOptions CreateLegacyCompatible()
        {
            return new ConsoleAppOptions()
            {
                StrictOption = false,
                NoAttributeCommandAsImplicitlyDefault = true
            };
        }
    }
}