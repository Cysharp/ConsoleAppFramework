﻿using System;

namespace ConsoleAppFramework
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field, AllowMultiple = false)]
    public class OptionAttribute : Attribute
    {
        public int Index { get; }
        public string? ShortName { get; }
        public string? Description { get; }

        /// <summary>Override default value on help.</summary>
        public string? DefaultValue { get; set; }

        public OptionAttribute(int index)
        {
            this.Index = index;
            this.Description = null;
        }

        public OptionAttribute(int index, string description)
        {
            this.Index = index;
            this.Description = description;
        }

        public OptionAttribute(string shortName)
        {
            this.Index = -1;
            this.ShortName = string.IsNullOrWhiteSpace(shortName) ? null : shortName;
            this.Description = null;
        }

        public OptionAttribute(string? shortName, string description)
        {
            this.Index = -1;
            this.ShortName = string.IsNullOrWhiteSpace(shortName) ? null : shortName;
            this.Description = description;
        }
    }
}
