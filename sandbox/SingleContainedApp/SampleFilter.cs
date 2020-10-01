using ConsoleAppFramework;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SingleContainedApp
{
    public class LogRunningTimeFilter : ConsoleAppFilter
    {
        public override async ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next)
        {
            context.Logger.LogInformation("Call method at " + context.Timestamp.ToLocalTime()); // LocalTime for human readable time
            try
            {
                await next(context);
                context.Logger.LogInformation("Call method Completed successfully, Elapsed:" + (DateTimeOffset.UtcNow - context.Timestamp));
            }
            catch
            {
                context.Logger.LogInformation("Call method Completed Failed, Elapsed:" + (DateTimeOffset.UtcNow - context.Timestamp));
                throw;
            }
        }
    }

    public class MutexFilter : ConsoleAppFilter
    {
        public override async ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next)
        {
            using (var mutex = new Mutex(false, context.MethodInfo.Name))
            {
                if (!mutex.WaitOne(0, false))
                {
                    throw new Exception($"already running {context.MethodInfo.Name} in another process.");
                }

                try
                {
                    await next(context);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }
    }
}
