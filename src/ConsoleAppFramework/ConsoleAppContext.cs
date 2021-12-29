using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace ConsoleAppFramework
{
    public class ConsoleAppContext
    {
        public string?[] Arguments { get; }
        public DateTime Timestamp { get; }
        public CancellationToken CancellationToken { get; }
        public ILogger<ConsoleApp> Logger { get; }
        public MethodInfo MethodInfo { get; }
        public IServiceProvider ServiceProvider { get; }
        public IDictionary<string, object> Items { get; }

        public ConsoleAppContext(string?[] arguments, DateTime timestamp, CancellationToken cancellationToken, ILogger<ConsoleApp> logger, MethodInfo methodInfo, IServiceProvider serviceProvider)
        {
            Arguments = arguments;
            Timestamp = timestamp;
            CancellationToken = cancellationToken;
            Logger = logger;
            MethodInfo = methodInfo;
            ServiceProvider = serviceProvider;
            Items = new Dictionary<string, object>();
        }
    }
}
