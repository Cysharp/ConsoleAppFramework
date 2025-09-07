using ConsoleAppFramework;

args = ["hello"];

var builder = ConsoleApp.Create();

builder.UseFilter<NopFilter1>();
builder.UseFilter<NopFilter2>();

builder.Add<MyClass>();

await builder.RunAsync(args);

[ConsoleAppFilter<NopFilter3>]
[ConsoleAppFilter<NopFilter4>]
public class MyClass
{
    [ConsoleAppFilter<NopFilter5>]
    [ConsoleAppFilter<NopFilter6>]
    public void Hello()
    {
        Console.Write("abcde");
    }
}

internal class NopFilter1(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.Write(1);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter2(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.Write(2);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter3(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.Write(3);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter4(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.Write(4);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter5(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.Write(5);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter6(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.Write(6);
        return Next.InvokeAsync(context, cancellationToken);
    }
}
