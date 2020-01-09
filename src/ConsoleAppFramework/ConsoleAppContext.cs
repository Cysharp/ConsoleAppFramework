using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace ConsoleAppFramework
{
    public class ConsoleAppContext
    {
        public string?[] Arguments { get; private set; }
        public DateTime Timestamp { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public ILogger<ConsoleAppEngine> Logger { get; private set; }

        public ConsoleAppContext(string?[] arguments, DateTime timestamp, CancellationToken cancellationToken, ILogger<ConsoleAppEngine> logger)
        {
            Arguments = arguments;
            Timestamp = timestamp;
            CancellationToken = cancellationToken;
            Logger = logger;
        }
    }
}
