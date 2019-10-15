using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace MicroBatchFramework
{
    internal class EmptyHostedService : IHostedService
    {
        private readonly IApplicationLifetime appLifetime;

        public EmptyHostedService(IApplicationLifetime appLifetime)
        {
            this.appLifetime = appLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this.appLifetime.StopApplication();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
