
using ConsoleAppFramework;
using System.ComponentModel.DataAnnotations;

// using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;

namespace GeneratorSandbox;

// ReadMe sample filters

internal class NopFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        try
        {
            /* on before */
            await Next.InvokeAsync(context, cancellationToken); // next
            /* on after */
        }
        catch
        {
            /* on error */
            throw;
        }
        finally
        {
            /* on finally */
        }
    }
}



internal class AuthenticationFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid();
        var userId = await GetUserIdAsync();

        // setup new state to context
        var authedContext = context with { State = new ApplicationContext(requestId, userId) };
        await Next.InvokeAsync(authedContext, cancellationToken);
    }

    // get user-id from DB/auth saas/others
    async Task<int> GetUserIdAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        return 1999;
    }
}

record class ApplicationContext(Guid RequestId, int UserId);

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

internal class PreventMultipleSameCommandInvokeFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        var basePath = Assembly.GetEntryAssembly()?.Location.Replace(Path.DirectorySeparatorChar, '_');
        var mutexKey = $"{basePath}$$${context.CommandName}"; // lock per command-name

        using var mutex = new Mutex(true, mutexKey, out var createdNew);
        if (!createdNew)
        {
            throw new Exception($"already running command:{context.CommandName} in another process.");
        }

        await Next.InvokeAsync(context, cancellationToken);
    }
}


//internal class ServiceProviderScopeFilter(IServiceProvider serviceProvider, ConsoleAppFilter next) : ConsoleAppFilter(next)
//{
//    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
//    {
//        // create Microsoft.Extensions.DependencyInjection scope
//        await using var scope = serviceProvider.CreateAsyncScope();
//        await Next.InvokeAsync(context, cancellationToken);
//    }
//}