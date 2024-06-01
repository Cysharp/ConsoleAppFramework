using ConsoleAppFramework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GeneratorSandbox;





internal class NopFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        return Next.InvokeAsync(context, cancellationToken);
    }
}


internal class LogRunningTimeFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        var startTime = Stopwatch.GetTimestamp();
        ConsoleApp.Log($"Execute command at {DateTime.UtcNow.ToLocalTime()}"); // LocalTime for human readable time
        try
        {
            await Next.InvokeAsync(context, cancellationToken);
            ConsoleApp.Log($"Command execute successfully at {DateTime.UtcNow.ToLocalTime()}, Elapsed: " + (Stopwatch.GetElapsedTime(startTime)));
        }
        catch
        {
            ConsoleApp.Log($"Command execute failed at {DateTime.UtcNow.ToLocalTime()}, Elapsed: " + (Stopwatch.GetElapsedTime(startTime)));
            throw;
        }
    }
}


internal class ChangeExitCodeFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        try
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException) return;

            Environment.ExitCode = 9999; // change custom exit code
            ConsoleApp.LogError(ex.ToString());
        }
    }
}

internal class PreventMultipleInstanceFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        // allow another command
        // prevent: location + command
        var basePath = Assembly.GetEntryAssembly()?.Location.Replace(Path.DirectorySeparatorChar, '_');

        var mutexKey = $"{basePath}$$${context.CommandName}";

        using var mutex = new Mutex(true, mutexKey, out var createdNew);
        if (!createdNew)
        {
            throw new Exception($"already running command:{context.CommandName} in another process.");
        }

        await Next.InvokeAsync(context, cancellationToken);
    }
}