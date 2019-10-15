using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MicroBatchFramework
{
    public interface IBatchInterceptor
    {
        /// <summary>
        /// Called once when BatchEngineService is stareted.
        /// </summary>
        ValueTask OnBatchEngineBeginAsync(IServiceProvider serviceProvider, ILogger<BatchEngine> logger);

        /// <summary>
        /// Called once when BatchEngineService is finished.
        /// </summary>
        ValueTask OnBatchEngineEndAsync();

        /// <summary>
        /// Called when BatchMethod is called.
        /// </summary>
        ValueTask OnBatchRunBeginAsync(BatchContext context);

        /// <summary>
        /// Called when BatchMethod is error or completed.
        /// </summary>
        ValueTask OnBatchRunCompleteAsync(BatchContext context, string errorMessageIfFailed, Exception exceptionIfExists);
    }

    public class NullBatchInterceptor : IBatchInterceptor
    {
        public static readonly IBatchInterceptor Default = new NullBatchInterceptor();
        private readonly ValueTask Empty = default(ValueTask);

        public ValueTask OnBatchEngineBeginAsync(IServiceProvider serviceProvider, ILogger<BatchEngine> logger)
        {
            return Empty;
        }

        public ValueTask OnBatchEngineEndAsync()
        {
            return Empty;
        }

        public ValueTask OnBatchRunBeginAsync(BatchContext context)
        {
            return Empty;
        }

        public ValueTask OnBatchRunCompleteAsync(BatchContext context, string errorMessageIfFailed, Exception exceptionIfExists)
        {
            return Empty;
        }
    }
}
