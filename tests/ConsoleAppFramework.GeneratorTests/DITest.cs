namespace ConsoleAppFramework.GeneratorTests;

public class DITest(ITestOutputHelper output)
{
    VerifyHelper verifier = new VerifyHelper(output, "CAF");

    [Fact]
    public void ServiceProvider()
    {
        verifier.Execute("""
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
}
