using System;
using System.Collections.Generic;
using System.Text;

namespace MicroBatchFramework
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class OptionAttribute : Attribute
    {
        public string ShortName { get; private set; }
        public string Description { get; private set; }

        public OptionAttribute(string shortName)
        {
            this.ShortName = shortName;
            this.Description = null;
        }

        public OptionAttribute(string shortName, string description)
        {
            this.ShortName = shortName;
            this.Description = description;
        }
    }
}
