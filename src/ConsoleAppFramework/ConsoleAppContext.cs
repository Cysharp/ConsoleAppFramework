using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace ConsoleAppFramework
{
    public class ConsoleAppContext
    {
        readonly CancellationTokenSource cancellationTokenSource;

        public string?[] Arguments { get; }
        public DateTime Timestamp { get; }
        public CancellationToken CancellationToken { get; }
        public ILogger<ConsoleApp> Logger { get; }
        public MethodInfo MethodInfo { get; }
        public IServiceProvider ServiceProvider { get; }
        public IDictionary<string, object> Items { get; }

        public ConsoleAppContext(string?[] arguments, DateTime timestamp, CancellationTokenSource cancellationTokenSource, ILogger<ConsoleApp> logger, MethodInfo methodInfo, IServiceProvider serviceProvider)
        {
            this.cancellationTokenSource = cancellationTokenSource;
            Arguments = arguments;
            Timestamp = timestamp;
            CancellationToken = cancellationTokenSource.Token;
            Logger = logger;
            MethodInfo = methodInfo;
            ServiceProvider = serviceProvider;
            Items = new Dictionary<string, object>();
        }

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }

        public void Terminate()
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Token.ThrowIfCancellationRequested();
        }
    }
}
