using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;

namespace ConsoleAppFramework
{
    public class ConsoleAppOptions
    {
        /// <summary>
        /// Argument parser uses strict(-short, --long) option. Default is true.
        /// </summary>
        public bool StrictOption { get; set; } = true; // TODO: legacy compatibility => false;

        /// <summary>
        /// Show default command(help/version) to help. Default is true.
        /// </summary>
        public bool ShowDefaultCommand { get; set; } = true;

        public JsonSerializerOptions? JsonSerializerOptions { get; set; }

        public ConsoleAppFilter[]? GlobalFilters { get; set; }

        // TODO: Legacy Compatibility options
        // NoAttributeCommandAsImplicitlyDefault



        // internal store values for execute engine.

        internal string[] CommandLineArguments { get; set; } = default!;
        internal CommandDescriptorCollection CommandDescriptors { get; } = new CommandDescriptorCollection();
    }

    // TODO:mitaina...
    public class LegacyCompatibilityOptions
    {
        public bool NoAttributeCommandAsImplicitlyDefault { get; set; }
    }
}