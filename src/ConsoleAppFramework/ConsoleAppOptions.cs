using System;
using System.Text;
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
        public bool ReplaceToUseSimpleConsoleLogger { get; set; } = true;
        public JsonSerializerOptions? JsonSerializerOptions { get; set; }

        public ConsoleAppFilter[]? GlobalFilters { get; set; }

        public bool NoAttributeCommandAsImplicitlyDefault { get; set; }

        public Func<string, string> NameConverter { get; set; } = KebabCaseConvert;

        public bool HelpSortCommandsByFullName { get; set; } = false;

        public string? ApplicationName { get; set; } = null;

        // internal store values for execute engine.

        internal string[] CommandLineArguments { get; set; } = default!;
        internal CommandDescriptorCollection CommandDescriptors { get; }

        public ConsoleAppOptions()
        {
            CommandDescriptors = new CommandDescriptorCollection(this);
        }

        public static ConsoleAppOptions CreateLegacyCompatible()
        {
            return new ConsoleAppOptions()
            {
                StrictOption = false,
                NoAttributeCommandAsImplicitlyDefault = true,
                NameConverter = x => x.ToLower(),
                ReplaceToUseSimpleConsoleLogger = false
            };
        }

        static string KebabCaseConvert(string name)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                if (!Char.IsUpper(name[i]))
                {
                    sb.Append(name[i]);
                    continue;
                }

                // Abc, abC, AB-c => first or Last or capital continuous, no added.
                if (i == 0 || i == name.Length - 1 || Char.IsUpper(name[i + 1]))
                {
                    sb.Append(Char.ToLowerInvariant(name[i]));
                    continue;
                }

                // others, add-
                sb.Append('-');
                sb.Append(Char.ToLowerInvariant(name[i]));
            }
            return sb.ToString();
        }
    }
}