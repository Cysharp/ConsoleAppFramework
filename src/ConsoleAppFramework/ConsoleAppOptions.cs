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
        /// Argument parser uses strict(-short, --long) option. Default is false.
        /// </summary>
        public bool StrictOption { get; set; } = false;

        /// <summary>
        /// Show default command(help/version) to help. Default is true.
        /// </summary>
        public bool ShowDefaultCommand { get; set; } = true;

        public JsonSerializerOptions? JsonSerializerOptions { get; set; }

        public ConsoleAppFilter[]? GlobalFilters { get; set; }

        // internal store values for execute engine.

        internal string[] CommandLineArguments { get; set; } = default!;
        internal CommandDescriptorMatcher CommandDescriptors { get; } = new CommandDescriptorMatcher();
    }
}