using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleAppFramework
{
    public sealed class ConsoleAppEngineService : IHostedService
    {
        string[] args;
        Type type;
        MethodInfo? methodInfo;
        IHostApplicationLifetime appLifetime;
        ILogger<ConsoleAppEngine> logger;
        IServiceScope scope;
        IConsoleAppInterceptor interceptor;
        Task? runningTask;
        CancellationTokenSource cancellationTokenSource;

        public ConsoleAppEngineService(IHostApplicationLifetime appLifetime, Type type, string[] args, ILogger<ConsoleAppEngine> logger, IServiceProvider provider)
            : this(appLifetime, type, null, args, logger, provider)
        {
        }

        public ConsoleAppEngineService(IHostApplicationLifetime appLifetime, Type type, MethodInfo? methodInfo, string[] args, ILogger<ConsoleAppEngine> logger, IServiceProvider provider)
        {
            this.args = args;
            this.type = type;
            this.methodInfo = methodInfo;
            this.appLifetime = appLifetime;
            this.scope = provider.CreateScope();
            this.logger = logger;
            this.interceptor = (provider.GetService(typeof(IConsoleAppInterceptor)) as IConsoleAppInterceptor) ?? NullConsoleAppInterceptor.Default;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await interceptor.OnEngineBeginAsync(scope.ServiceProvider, logger);

            // raise after all event registered
            appLifetime.ApplicationStarted.Register(async state =>
            {
                var self = (ConsoleAppEngineService)state;
                try
                {
                    var engine = new ConsoleAppEngine(self.logger, scope.ServiceProvider, self.interceptor, self.cancellationTokenSource.Token);
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
                    logger.LogTrace("Detect Cancel signal, wait for running console app task canceled.");
                    await task;
                    logger.LogTrace("ConsoleApp cancel completed.");
                }
            }
            finally
            {
                await interceptor.OnEngineCompleteAsync(this.scope.ServiceProvider, logger);
                scope.Dispose();
            }
        }
    }
}
