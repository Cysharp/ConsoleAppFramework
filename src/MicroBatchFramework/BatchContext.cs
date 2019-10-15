using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace MicroBatchFramework
{
    public class BatchContext
    {
        public string[] Arguments { get; }
        public DateTime Timestamp { get; }
        public CancellationToken CancellationToken { get; }
        public ILogger<BatchEngine> Logger { get; }

        public BatchContext(string[] arguments, DateTime timestamp, CancellationToken cancellationToken, ILogger<BatchEngine> logger)
        {
            Arguments = arguments;
            Timestamp = timestamp;
            CancellationToken = cancellationToken;
            Logger = logger;
        }
    }
}
