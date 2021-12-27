using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleAppFramework
{
    // This servcie is called from ConsoleApp.Run
    public sealed class ConsoleAppEngineService : IHostedService
    {
        IHostApplicationLifetime appLifetime;
        ILogger<ConsoleApp> logger;
        IServiceProvider provider;
        IServiceScope? scope;
        Task? runningTask;
        CancellationTokenSource cancellationTokenSource;

        public ConsoleAppEngineService(IHostApplicationLifetime appLifetime, ILogger<ConsoleApp> logger, IServiceProvider provider)
        {
            this.appLifetime = appLifetime;
            this.provider = provider;
            this.logger = logger;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken _) // TODO: use _(this is used ShutdownTimeout ??? )
        {
            // raise after all event registered
            appLifetime.ApplicationStarted.Register(async state =>
            {
                var self = (ConsoleAppEngineService)state!;
                try
                {
                    self.scope = self.provider.CreateScope();
                    var engine = ActivatorUtilities.CreateInstance<ConsoleAppEngine>(self.scope.ServiceProvider, self.cancellationTokenSource.Token);
                    self.runningTask = engine.RunAsync();
                    await self.runningTask;
                    self.runningTask = null;
                }
                catch
                {
                    // don't do anything.
                }
                finally
                {
                    self.appLifetime.StopApplication();
                }
            }, this);

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken _) // TODO:use _???
        {
            try
            {
                cancellationTokenSource?.Cancel();

                var task = runningTask;
                if (task != null)
                {
                    logger.LogTrace("Detect Cancel signal, wait for running console app task canceled.");
                    // TODO:require timeout.
                    await task;
                    logger.LogTrace("ConsoleApp cancel completed.");
                }
            }
            finally
            {
                if (scope is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else
                {
                    scope?.Dispose();
                }
            }
        }
    }
}