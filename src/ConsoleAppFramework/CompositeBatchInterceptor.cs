using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ConsoleAppFramework
{
    public class CompositeBatchInterceptor : IBatchInterceptor
    {
        readonly IBatchInterceptor[] interceptors;

        public CompositeBatchInterceptor(params IBatchInterceptor[] interceptors)
        {
            this.interceptors = interceptors;
        }

        public async ValueTask OnBatchEngineBeginAsync(IServiceProvider serviceProvider, ILogger<BatchEngine> logger)
        {
            var exceptions = new AggregateExceptionHolder();
            foreach (var item in interceptors)
            {
                try
                {
                    await item.OnBatchEngineBeginAsync(serviceProvider, logger);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }
            exceptions.ThrowIfExists();
        }

        public async ValueTask OnBatchEngineEndAsync()
        {
            var exceptions = new AggregateExceptionHolder();
            foreach (var item in interceptors)
            {
                try
                {
                    await item.OnBatchEngineEndAsync();
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }
            exceptions.ThrowIfExists();
        }

        public async ValueTask OnBatchRunBeginAsync(BatchContext context)
        {
            var exceptions = new AggregateExceptionHolder();
            foreach (var item in interceptors)
            {
                try
                {
                    await item.OnBatchRunBeginAsync(context);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }
            exceptions.ThrowIfExists();
        }

        public async ValueTask OnBatchRunCompleteAsync(BatchContext context, string? errorMessageIfFailed, Exception? exceptionIfExists)
        {
            var exceptions = new AggregateExceptionHolder();
            foreach (var item in interceptors)
            {
                try
                {
                    await item.OnBatchRunCompleteAsync(context, errorMessageIfFailed, exceptionIfExists);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }
            exceptions.ThrowIfExists();
        }
    }

    internal struct AggregateExceptionHolder
    {
        List<ExceptionDispatchInfo> exceptions;

        public void Add(Exception ex)
        {
            if (exceptions == null) exceptions = new List<ExceptionDispatchInfo>();
            exceptions.Add(ExceptionDispatchInfo.Capture(ex));
        }

        public void ThrowIfExists()
        {
            if (exceptions == null) return;

            if (exceptions.Count == 1)
            {
                exceptions[0].Throw();
            }
            else
            {
                throw new AggregateException(exceptions.Select(x => x.SourceException));
            }
        }
    }
}
