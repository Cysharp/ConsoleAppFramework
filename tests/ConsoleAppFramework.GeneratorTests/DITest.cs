namespace ConsoleAppFramework.GeneratorTests;

[ClassDataSource<VerifyHelper>]
public class DITest(VerifyHelper verifier)
{
    [Test]
    public async Task ServiceProvider()
    {
        await verifier.Execute("""
#nullable enable

var di = new MiniDI();
di.Register(typeof(MyClass), new MyClass("foo"));
ConsoleApp.ServiceProvider = di;

ConsoleApp.Run(args, ([FromServices] MyClass mc, int x, int y) => { Console.Write(mc.Name + ":" + x + ":" + y); });


class MiniDI : IServiceProvider
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

class MyClass(string name) 
{
    public string Name => name;
}
""", args: "--x 10 --y 20", expected: "foo:10:20");
    }

    [Test]
    public async Task WithFilter()
    {
        await verifier.Execute("""
var app = ConsoleApp.Create();
app.UseFilter<MyFilter>();
app.Run(["cmd", "test"]);

public class MyService
{
    public void Test() => Console.Write("Test");
}

internal class MyFilter(ConsoleAppFilter next, MyService myService) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        myService.Test();
        await Next.InvokeAsync(context, cancellationToken);
    }
}

[RegisterCommands("cmd")]
public class MyCommand
{
    [Command("test")]
    public int Test()
    {
        return 1;
    }
}

class MiniDI : IServiceProvider
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

namespace ConsoleAppFramework
{
    partial class ConsoleApp
    {
        partial class ConsoleAppBuilder
        {
            partial void BuildAndSetServiceProvider(ConsoleAppContext context)
            {
                var di = new MiniDI();
                di.Register(typeof(MyService), new MyService());
                ConsoleApp.ServiceProvider = di;
            }
        }
    }
}
""", "cmd test", "Test");
    }
}
