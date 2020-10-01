using System.Text.Json;

namespace ConsoleAppFramework
{
    public class ConsoleAppOptions
    {
        /// <summary>
        /// Argument parser uses strict(-short, --long) option. Default is false.
        /// </summary>
        public bool StrictOption { get; set; } = false;

        /// <summary>
        /// Show default command(help/version) to help. Default is true.
        /// </summary>
        public bool ShowDefaultCommand { get; set; } = true;

        public JsonSerializerOptions? JsonSerializerOptions { get; set; }

        public ConsoleAppFilter[]? GlobalFilters { get; set; }
    }
}