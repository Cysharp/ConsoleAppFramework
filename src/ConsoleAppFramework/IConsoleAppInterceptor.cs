using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ConsoleAppFramework
{
    public interface IConsoleAppInterceptor
    {
        /// <summary>
        /// Called once when ConsoleAppEngineService is stareted.
        /// </summary>
        ValueTask OnConsoleAppEngineBeginAsync(IServiceProvider serviceProvider, ILogger<ConsoleAppEngine> logger);

        /// <summary>
        /// Called once when ConsoleAppEngineService is finished.
        /// </summary>
        ValueTask OnConsoleAppEngineEndAsync();

        /// <summary>
        /// Called when ConsoleAppMethod is called.
        /// </summary>
        ValueTask OnConsoleAppRunBeginAsync(ConsoleAppContext context);

        /// <summary>
        /// Called when ConsoleAppMethod is error or completed.
        /// </summary>
        ValueTask OnConsoleAppRunCompleteAsync(ConsoleAppContext context, string? errorMessageIfFailed, Exception? exceptionIfExists);
    }

    public class NullConsoleAppInterceptor : IConsoleAppInterceptor
    {
        public static readonly IConsoleAppInterceptor Default = new NullConsoleAppInterceptor();
        readonly ValueTask Empty = default(ValueTask);

        public ValueTask OnConsoleAppEngineBeginAsync(IServiceProvider serviceProvider, ILogger<ConsoleAppEngine> logger)
        {
            return Empty;
        }

        public ValueTask OnConsoleAppEngineEndAsync()
        {
            return Empty;
        }

        public ValueTask OnConsoleAppRunBeginAsync(ConsoleAppContext context)
        {
            return Empty;
        }

        public ValueTask OnConsoleAppRunCompleteAsync(ConsoleAppContext context, string? errorMessageIfFailed, Exception? exceptionIfExists)
        {
            return Empty;
        }
    }
}
