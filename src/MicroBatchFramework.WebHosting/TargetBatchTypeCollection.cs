using System;
using System.Collections;
using System.Collections.Generic;

namespace MicroBatchFramework.WebHosting
{
    public class TargetBatchTypeCollection : IEnumerable<Type>
    {
        private readonly IEnumerable<Type> types;

        public TargetBatchTypeCollection(IEnumerable<Type> types)
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