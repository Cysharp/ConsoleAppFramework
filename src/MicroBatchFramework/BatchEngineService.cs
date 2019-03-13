using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace MicroBatchFramework
{
    public sealed class BatchEngineService : IHostedService
    {
        string[] args;
        Type type;
        MethodInfo methodInfo;
        IApplicationLifetime appLifetime;
        ILogger<BatchEngine> logger;
        IServiceProvider provider;
        IBatchInterceptor interceptor;
        Task runningTask;
        CancellationTokenSource cancellationTokenSource;

        public BatchEngineService(IApplicationLifetime appLifetime, Type type, string[] args, ILogger<BatchEngine> logger, IServiceProvider provider)
            : this(appLifetime, type, null, args, logger, provider)
        {
        }

        public BatchEngineService(IApplicationLifetime appLifetime, Type type, MethodInfo methodInfo, string[] args, ILogger<BatchEngine> logger, IServiceProvider provider)
        {
            this.args = args;
            this.type = type;
            this.methodInfo = methodInfo;
            this.appLifetime = appLifetime;
            this.provider = provider;
            this.logger = logger;
            this.interceptor = (provider.GetService(typeof(IBatchInterceptor)) as IBatchInterceptor) ?? NullBatchInerceptor.Default;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await interceptor.OnBatchEngineBeginAsync(provider, logger);

            // raise after all event registered
            appLifetime.ApplicationStarted.Register(async state =>
            {
                var self = (BatchEngineService)state;
                try
                {
                    var engine = new BatchEngine(self.logger, self.provider, self.interceptor, self.cancellationTokenSource.Token);
                    if (self.methodInfo != null)
                    {
                        self.runningTask = engine.RunAsync(self.type, self.methodInfo, self.args);
                    }
                    else
                    {
                        self.runningTask = engine.RunAsync(self.type, self.args);
                    }

                    await self.runningTask;
                    self.runningTask = null;
                }
                catch { } // don't do anything.
                finally
                {
                    self.appLifetime.StopApplication();
                }
            }, this);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationTokenSource?.Cancel();

                var task = runningTask;
                if (task != null)
                {
                    logger.LogTrace("Detect Cancel signal, wait for running batch task canceled.");
                    await task;
                    logger.LogTrace("Batch cancel completed.");
                }
            }
            finally
            {
                await interceptor.OnBatchEngineEndAsync();
            }
        }
    }
}
