using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace MicroBatchFramework.Logging
{
    public class SimpleConsoleLoggerProvider : ILoggerProvider
    {
        readonly SimpleConsoleLogger logger;

        public SimpleConsoleLoggerProvider(IOptions<SimpleConsoleLoggerOption> option)
        {
            logger = new SimpleConsoleLogger(option.Value.MinLogLevel);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return logger;
        }

        public void Dispose()
        {
        }
    }

    public class SimpleConsoleLogger : ILogger
    {
        readonly LogLevel minLogLevel;

        public SimpleConsoleLogger(LogLevel minLogLevel)
        {
            this.minLogLevel = minLogLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel == LogLevel.None) return false;
            return (int)logLevel >= (int)this.minLogLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));

            var msg = formatter(state, exception);


            if (!string.IsNullOrEmpty(msg))
            {
                Console.WriteLine(msg);
            }

            if (exception != null)
            {
                Console.WriteLine(exception.ToString());
            }
        }

        class NullDisposable : IDisposable
        {
            public static readonly IDisposable Instance = new NullDisposable();

            public void Dispose()
            {
            }
        }
    }

    public class SimpleConsoleLoggerOption
    {
        public LogLevel MinLogLevel { get; set; } = LogLevel.Trace; // default is trace(should use another filter). 
    }

    public static class SimpleConsoleLoggerExtensions
    {
        public static ILoggingBuilder AddSimpleConsole(this ILoggingBuilder builder)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SimpleConsoleLoggerProvider>());
            return builder;
        }
    }
}