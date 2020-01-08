using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace ConsoleAppFramework
{
    public class CompositeConsoleAppInterceptor : IConsoleAppInterceptor
    {
        readonly IConsoleAppInterceptor[] interceptors;

        public CompositeConsoleAppInterceptor(params IConsoleAppInterceptor[] interceptors)
        {
            this.interceptors = interceptors;
        }

        public async ValueTask OnConsoleAppEngineBeginAsync(IServiceProvider serviceProvider, ILogger<ConsoleAppEngine> logger)
        {
            var exceptions = new AggregateExceptionHolder();
            foreach (var item in interceptors)
            {
                try
                {
                    await item.OnConsoleAppEngineBeginAsync(serviceProvider, logger);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }
            exceptions.ThrowIfExists();
        }

        public async ValueTask OnConsoleAppEngineEndAsync()
        {
            var exceptions = new AggregateExceptionHolder();
            foreach (var item in interceptors)
            {
                try
                {
                    await item.OnConsoleAppEngineEndAsync();
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }
            exceptions.ThrowIfExists();
        }

        public async ValueTask OnConsoleAppRunBeginAsync(ConsoleAppContext context)
        {
            var exceptions = new AggregateExceptionHolder();
            foreach (var item in interceptors)
            {
                try
                {
                    await item.OnConsoleAppRunBeginAsync(context);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }
            exceptions.ThrowIfExists();
        }

        public async ValueTask OnConsoleAppRunCompleteAsync(ConsoleAppContext context, string? errorMessageIfFailed, Exception? exceptionIfExists)
        {
            var exceptions = new AggregateExceptionHolder();
            foreach (var item in interceptors)
            {
                try
                {
                    await item.OnConsoleAppRunCompleteAsync(context, errorMessageIfFailed, exceptionIfExists);
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
