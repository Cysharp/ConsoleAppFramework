using System;
using System.Threading.Tasks;

namespace MicroBatchFramework
{
    public interface IBatchInterceptor
    {
        ValueTask OnBatchEngineBegin();
        ValueTask OnBatchEngineEnd();
        ValueTask OnBatchRunBegin(BatchContext context);
        ValueTask OnBatchRunComplete(BatchContext context, string errorMessageIfFailed, Exception exceptionIfExists);
    }

    public class NullBatchInerceptor : IBatchInterceptor
    {
        public static readonly IBatchInterceptor Default = new NullBatchInerceptor();
        readonly ValueTask Empty = default(ValueTask);

        public ValueTask OnBatchEngineBegin()
        {
            return Empty;
        }

        public ValueTask OnBatchEngineEnd()
        {
            return Empty;
        }

        public ValueTask OnBatchRunBegin(BatchContext context)
        {
            return Empty;
        }

        public ValueTask OnBatchRunComplete(BatchContext context, string errorMessageIfFailed, Exception exceptionIfExists)
        {
            return Empty;
        }
    }
}
