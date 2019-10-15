using FluentAssertions;
using FluentAssertions.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace MicroBatchFramework.Tests
{
    public static class LoggingExtensions
    {
        public static IHostBuilder ConfigureTestLogging(this IHostBuilder builder, ITestOutputHelper testOutputHelper, LogStack logStack, bool throwExceptionOnError)
        {
            return builder
                .ConfigureServices(x => x.AddSingleton<LogStack>(logStack))
                .ConfigureLogging(x => x.SetMinimumLevel(LogLevel.Trace).AddProvider(new XUnitLoggerProvider(testOutputHelper, logStack, throwExceptionOnError)));
        }
    }

    public class TextWriterBridge : TextWriter
    {
        readonly ITestOutputHelper helper;
        readonly LogStack logStack;

        public TextWriterBridge(ITestOutputHelper helper, LogStack logStack)
        {
            this.helper = helper;
            this.logStack = logStack;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void Write(string value)
        {
            logStack.Add(LogLevel.Information, value);
            helper.WriteLine(value);
        }

        public static IDisposable BeginSetConsoleOut(ITestOutputHelper helper, LogStack logStack)
        {
            var current = Console.Out;
            Console.SetOut(new TextWriterBridge(helper, logStack));
            return new Scope(current);
        }

        public struct Scope : IDisposable
        {
            private readonly TextWriter writer;

            public Scope(TextWriter writer)
            {
                this.writer = writer;
            }

            public void Dispose()
            {
                Console.SetOut(writer);
            }
        }
    }

    public class LogStack
    {
        readonly Dictionary<LogLevel, List<string>> logs = new Dictionary<LogLevel, List<string>>(6);

        public LogStack()
        {
            logs.Add(LogLevel.Trace, new List<string>());
            logs.Add(LogLevel.Warning, new List<string>());
            logs.Add(LogLevel.Information, new List<string>());
            logs.Add(LogLevel.Debug, new List<string>());
            logs.Add(LogLevel.Critical, new List<string>());
            logs.Add(LogLevel.Error, new List<string>());
        }

        public void Add(LogLevel level, string msg)
        {
            logs[level].Add(msg);
        }

        public List<string> InfoLog => logs[LogLevel.Information];

        public StringAssertions InfoLogShould(int index) => logs[LogLevel.Information][index].Should();
        public AndConstraint<StringAssertions> InfoLogShouldBe(int index, string expected) => InfoLogShould(index).Be(expected);

        public List<string> GetLogs(LogLevel level)
        {
            return logs[level];
        }

        public void ClearAll()
        {
            foreach (var item in logs)
            {
                item.Value.Clear();
            }
        }

        public string ToStringInfo() => ToString(LogLevel.Information);

        public string ToString(LogLevel level)
        {
            var sb = new StringBuilder();
            foreach (var item in GetLogs(level))
            {
                sb.AppendLine(item);
            }
            return sb.ToString();
        }
    }

    public class TestLogException : Exception
    {
        public TestLogException(string message) : base(message)
        {
        }

        public TestLogException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class XUnitLoggerProvider : ILoggerProvider
    {
        readonly XUnitLogger logger;

        public XUnitLoggerProvider(ITestOutputHelper testOutput, LogStack logStack, bool throwExceptionOnError)
        {
            logger = new XUnitLogger(testOutput, logStack, throwExceptionOnError);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return logger;
        }

        public void Dispose()
        {
        }
    }

    public class XUnitLogger : ILogger
    {
        readonly ITestOutputHelper testOutput;
        readonly LogStack logStack;
        readonly bool throwExceptionOnError;

        public XUnitLogger(ITestOutputHelper testOutput, LogStack logStack, bool throwExceptionOnError)
        {
            this.testOutput = testOutput;
            this.logStack = logStack;
            this.throwExceptionOnError = throwExceptionOnError;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NullDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var msg = formatter(state, exception);

            if (logLevel == LogLevel.Error && throwExceptionOnError)
            {
                throw (exception != null ? new TestLogException(msg, exception) : new TestLogException(msg));
            }

            if (!string.IsNullOrEmpty(msg))
            {
                logStack.Add(logLevel, msg);
                testOutput.WriteLine(msg);
            }

            if (exception != null)
            {
                logStack.Add(logLevel, exception.ToString());
                testOutput.WriteLine(exception.ToString());
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
