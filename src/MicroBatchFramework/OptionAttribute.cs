using System;
using System.Collections.Generic;
using System.Text;

namespace MicroBatchFramework
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class OptionAttribute : Attribute
    {
        public int Index { get; }
        public string ShortName { get; }
        public string Description { get; }

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
            this.ShortName = shortName;
            this.Description = null;
        }

        public OptionAttribute(string shortName, string description)
        {
            this.Index = -1;
            this.ShortName = shortName;
            this.Description = description;
        }
    }
}
