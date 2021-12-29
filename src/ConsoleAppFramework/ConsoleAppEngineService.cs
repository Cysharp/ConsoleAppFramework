using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleAppFramework
{
    // This servcie is called from ConsoleApp.Run
    internal sealed class ConsoleAppEngineService : IHostedService
    {
        IHostApplicationLifetime appLifetime;
        ILogger<ConsoleApp> logger;
        IServiceProvider provider;
        IServiceScope? scope;
        Task? runningTask;
        CancellationTokenSource? cancellationTokenSource;

        public ConsoleAppEngineService(IHostApplicationLifetime appLifetime, ILogger<ConsoleApp> logger, IServiceProvider provider, IOptionsMonitor<HostOptions> hostOptions)
        {
            this.appLifetime = appLifetime;
            this.provider = provider;
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken ct)
        {
            cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct);

            // raise after all event registered
            appLifetime.ApplicationStarted.Register(async state =>
            {
                var self = (ConsoleAppEngineService)state!;
                try
                {
                    self.scope = self.provider.CreateScope();
                    var token = (self.cancellationTokenSource != null) ? self.cancellationTokenSource.Token : CancellationToken.None;
                    var engine = ActivatorUtilities.CreateInstance<ConsoleAppEngine>(self.scope.ServiceProvider, token);
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

        public async Task StopAsync(CancellationToken ct)
        {
            try
            {
                cancellationTokenSource?.Cancel();

                var task = runningTask;
                if (task != null)
                {
                    logger.LogTrace("Detect Cancel signal, wait for running console app task canceled.");
                    try
                    {
                        if (ct.CanBeCanceled)
                        {
                            var cancelTask = CreateTimeoutTask(ct);
                            var completedTask = await Task.WhenAny(cancelTask, task);
                            if (completedTask == cancelTask)
                            {
                                logger.LogTrace("ConsoleApp aborted, cancel timeout.");
                            }
                            else
                            {
                                logger.LogTrace("ConsoleApp cancel completed.");
                            }
                        }
                        else
                        {
                            await task;
                            logger.LogTrace("ConsoleApp cancel completed.");
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        if (ex.CancellationToken == ct)
                        {
                            logger.LogTrace("ConsoleApp aborted, cancel timeout.");
                        }
                        else
                        {
                            logger.LogTrace("ConsoleApp cancel completed.");
                        }
                    }
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

        Task CreateTimeoutTask(CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<object>();
            ct.Register(() =>
            {
                tcs.TrySetCanceled(ct);
            });
            return tcs.Task;
        }
    }
}