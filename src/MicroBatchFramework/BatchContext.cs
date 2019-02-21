using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace MicroBatchFramework
{
    public class BatchContext
    {
        public string[] Arguments { get; private set; }
        public DateTime Timestamp { get; private set; }
        public CancellationToken CancellationToken { get; private set; }
        public ILogger<BatchEngine> Logger { get; private set; }

        public BatchContext(string[] arguments, DateTime timestamp, CancellationToken cancellationToken, ILogger<BatchEngine> logger)
        {
            Arguments = arguments;
            Timestamp = timestamp;
            CancellationToken = cancellationToken;
            Logger = logger;
        }
    }
}
