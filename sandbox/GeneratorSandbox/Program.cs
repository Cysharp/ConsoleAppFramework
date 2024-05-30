using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ConsoleAppFramework;

//args = ["--foo", "10", "--bar", "20"];


ConsoleApp.Run(args, (int foo, int bar) => Console.WriteLine($"Sum: {foo + bar}"));






[AttributeUsage(AttributeTargets.Parameter)]
public class Vector3ParserAttribute : Attribute, IArgumentParser<Vector3>
{
    public static bool TryParse(ReadOnlySpan<char> s, out Vector3 result)
    {
        Span<Range> ranges = stackalloc Range[3];
        var splitCount = s.Split(ranges, ',');
        if (splitCount != 3)
        {
            result = default;
            return false;
        }

        float x;
        float y;
        float z;
        if (float.TryParse(s[ranges[0]], out x) && float.TryParse(s[ranges[1]], out y) && float.TryParse(s[ranges[2]], out z))
        {
            result = new Vector3(x, y, z);
            return true;
        }

        result = default;
        return false;
    }
}



public class FilterContext : IServiceProvider
{
    public long Timestamp { get; set; }
    public Guid UserId { get; set; }

    object IServiceProvider.GetService(Type serviceType)
    {
        if (serviceType == typeof(FilterContext)) return this;
        throw new InvalidOperationException("Type is invalid:" + serviceType);
    }
}

internal class TimestampFilter(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("filter1");
        return Next.InvokeAsync(cancellationToken);
    }
}


internal class LogExecutionTimeFilter(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(CancellationToken cancellationToken)
    {
        var startingTime = Stopwatch.GetTimestamp();
        try
        {
            await Next.InvokeAsync(cancellationToken);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(startingTime);
            ConsoleApp.Log($"Execution Time: {elapsed}");
        }
    }
}

internal class NanimosinaiFilter(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("filter0");
        return Next.InvokeAsync(cancellationToken);
    }
}


public class MyContext : IServiceProvider
{
    public long Timestamp { get; set; }
    public Guid UserId { get; set; }

    object IServiceProvider.GetService(Type serviceType)
    {
        if (serviceType == typeof(MyContext)) return this;
        throw new InvalidOperationException("Type is invalid:" + serviceType);
    }
}
