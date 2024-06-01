using ConsoleAppFramework;

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
        var newContext = context with { State = 100 };

        Console.Write("invoke:");
        Console.Write(foo);
        Console.Write(bar);
        return Next.InvokeAsync(newContext, cancellationToken);
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