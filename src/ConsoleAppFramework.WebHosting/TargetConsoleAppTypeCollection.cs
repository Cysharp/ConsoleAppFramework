using System;
using System.Collections;
using System.Collections.Generic;

namespace ConsoleAppFramework.WebHosting
{
    public class TargetConsoleAppTypeCollection : IEnumerable<Type>
    {
        readonly IEnumerable<Type> types;

        public TargetConsoleAppTypeCollection(IEnumerable<Type> types)
        {
            this.types = types;
        }

        public IEnumerator<Type> GetEnumerator()
        {
            return types.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return types.GetEnumerator();
        }
    }
}