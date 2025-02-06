using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace ConsoleAppFramework.GeneratorTests;

public class FilterTest(ITestOutputHelper output)
{
    VerifyHelper verifier = new VerifyHelper(output, "CAF");

    [Fact]
    public void ForLambda()
    {
        verifier.Execute("""
var builder = ConsoleApp.Create();

builder.UseFilter<NopFilter1>();
builder.UseFilter<NopFilter2>();

builder.Add("", Hello);

builder.Run(args);

[ConsoleAppFilter<NopFilter3>]
[ConsoleAppFilter<NopFilter4>]
void Hello()
{
    Console.Write("abcde");
}

internal class NopFilter1(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context,CancellationToken cancellationToken)
    {
        Console.Write(1);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter2(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context,CancellationToken cancellationToken)
    {
        Console.Write(2);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter3(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context,CancellationToken cancellationToken)
    {
        Console.Write(3);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter4(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context,CancellationToken cancellationToken)
    {
        Console.Write(4);
        return Next.InvokeAsync(context, cancellationToken);
    }
}
""", args: "", expected: "1234abcde");
    }

    [Fact]
    public void ForClass()
    {
        verifier.Execute("""
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
    public override Task InvokeAsync(ConsoleAppContext context,CancellationToken cancellationToken)
    {
        Console.Write(1);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter2(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context,CancellationToken cancellationToken)
    {
        Console.Write(2);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter3(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context,CancellationToken cancellationToken)
    {
        Console.Write(3);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter4(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context,CancellationToken cancellationToken)
    {
        Console.Write(4);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter5(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context,CancellationToken cancellationToken)
    {
        Console.Write(5);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

internal class NopFilter6(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context,CancellationToken cancellationToken)
    {
        Console.Write(6);
        return Next.InvokeAsync(context, cancellationToken);
    }
}
""", args: "hello", expected: "123456abcde");
    }


    [Fact]
    public void DI()
    {
        verifier.Execute("""
var serviceCollection = new MiniDI();
serviceCollection.Register(typeof(string), "hoge!");
serviceCollection.Register(typeof(int), 9999);
ConsoleApp.ServiceProvider = serviceCollection;

var builder = ConsoleApp.Create();

builder.UseFilter<DIFilter>();

builder.Add("", () => Console.Write("do"));

builder.Run(args);

internal class DIFilter(string foo, int bar, ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.Write("invoke:");
        Console.Write(foo);
        Console.Write(bar);
        return Next.InvokeAsync(context, cancellationToken);
    }
}

public class MiniDI : IServiceProvider
{
    System.Collections.Generic.Dictionary<Type, object> dict = new();

    public void Register(Type type, object instance)
    {
        dict[type] = instance;
    }

    public object? GetService(Type serviceType)
    {
        return dict.TryGetValue(serviceType, out var instance) ? instance : null;
    }
}
""", args: "", expected: "invoke:hoge!9999do");
    }
}
