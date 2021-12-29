using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System;
using ConsoleAppFramework.Logging;

namespace ConsoleAppFramework.Logging
{
    public class SimpleConsoleLoggerProvider : ILoggerProvider
    {
        readonly SimpleConsoleLogger loggerDefault;
        readonly SimpleConsoleLogger loggerHostingInternal;

        public SimpleConsoleLoggerProvider()
        {
            loggerDefault = new SimpleConsoleLogger(LogLevel.Trace);
            loggerHostingInternal = new SimpleConsoleLogger(LogLevel.Information);
        }

        public ILogger CreateLogger(string categoryName)
        {
            // NOTE: It omits unimportant log messages from Microsoft.Extension.Hosting.Internal.*
            return categoryName.StartsWith("Microsoft.Extensions.Hosting.Internal")
                ? loggerHostingInternal
                : loggerDefault;
        }

        public void Dispose()
        {
        }
    }

    public class SimpleConsoleLogger : ILogger
    {
        readonly LogLevel minimumLogLevel;

        public SimpleConsoleLogger(LogLevel minimumLogLevel)
        {
            this.minimumLogLevel = minimumLogLevel;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return minimumLogLevel <= logLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (formatter == null) throw new ArgumentNullException(nameof(formatter));

            if (minimumLogLevel > logLevel) return;

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
}

namespace ConsoleAppFramework
{
    public static class SimpleConsoleLoggerExtensions
    {
        /// <summary>
        /// use ConsoleAppFramework.Logging.SimpleConsoleLogger.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ILoggingBuilder AddSimpleConsole(this ILoggingBuilder builder)
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SimpleConsoleLoggerProvider>());
            return builder;
        }

        /// <summary>
        /// Remove default ConsoleLoggerProvider and replace to SimpleConsoleLogger.
        /// </summary>
        public static ILoggingBuilder ReplaceToSimpleConsole(this ILoggingBuilder builder)
        {
            // Use SimpleConsoleLogger instead of the default ConsoleLogger.
            var consoleLogger = builder.Services.FirstOrDefault(x => x.ImplementationType?.FullName == "Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider");
            if (consoleLogger != null)
            {
                builder.Services.Remove(consoleLogger);
            }

            builder.AddSimpleConsole();
            return builder;
        }
    }
}