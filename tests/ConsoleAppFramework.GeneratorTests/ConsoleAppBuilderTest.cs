namespace ConsoleAppFramework.GeneratorTests;

public class ConsoleAppBuilderTest(ITestOutputHelper output) : IDisposable
{
    VerifyHelper verifier = new VerifyHelper(output, "CAF");

    public void Dispose() => Environment.ExitCode = 0;

    [Fact]
    public void BuilderRun()
    {
        var code = """
var builder = ConsoleApp.Create();
builder.Add("foo", (int x, int y) => { Console.Write(x + y); });
builder.Add("bar", (int x, int y = 10) => { Console.Write(x + y); });
builder.Add("baz", int (int x, string y) => { Console.Write(x + y); return 10; });
builder.Add("boz", async Task (int x) => { await Task.Yield(); Console.Write(x * 2); });
builder.Run(args);
""";

        verifier.Execute(code, "foo --x 10 --y 20", "30");
        verifier.Execute(code, "bar --x 20 --y 30", "50");
        verifier.Execute(code, "bar --x 20", "30");
        Environment.ExitCode.ShouldBe(0);
        verifier.Execute(code, "baz --x 40 --y takoyaki", "40takoyaki");
        Environment.ExitCode.ShouldBe(10);
        Environment.ExitCode = 0;

        verifier.Execute(code, "boz --x 40", "80");
    }

    [Fact]
    public void BuilderRunAsync()
    {
        var code = """
var builder = ConsoleApp.Create();
builder.Add("foo", (int x, int y) => { Console.Write(x + y); });
builder.Add("bar", (int x, int y = 10) => { Console.Write(x + y); });
builder.Add("baz", int (int x, string y) => { Console.Write(x + y); return 10; });
builder.Add("boz", async Task (int x) => { await Task.Yield(); Console.Write(x * 2); });
await builder.RunAsync(args);
""";

        verifier.Execute(code, "foo --x 10 --y 20", "30");
        verifier.Execute(code, "bar --x 20 --y 30", "50");
        verifier.Execute(code, "bar --x 20", "30");
        Environment.ExitCode.ShouldBe(0);
        verifier.Execute(code, "baz --x 40 --y takoyaki", "40takoyaki");
        Environment.ExitCode.ShouldBe(10);
        Environment.ExitCode = 0;

        verifier.Execute(code, "boz --x 40", "80");
    }

    [Fact]
    public void AddClass()
    {
        var code = """
var builder = ConsoleApp.Create();
builder.Add<MyClass>();
await builder.RunAsync(args);

public class MyClass
{
    public void Do()
    {
        Console.Write("yeah");
    }

    public void Sum(int x, int y)
    {
        Console.Write(x + y);
    }

    public void Echo(string msg)
    {
        Console.Write(msg);
    }

    void Echo()
    {
    }

    public static void Sum()
    {
    }
}
""";

        verifier.Execute(code, "do", "yeah");
        verifier.Execute(code, "sum --x 1 --y 2", "3");
        verifier.Execute(code, "echo --msg takoyaki", "takoyaki");
    }

    [Fact]
    public void ClassDispose()
    {
        verifier.Execute("""
var builder = ConsoleApp.Create();
builder.Add<MyClass>();
builder.Run(args);

public class MyClass : IDisposable
{
    public void Do()
    {
        Console.Write("yeah:");
    }

    public void Dispose()
    {
        Console.Write("disposed!");
    }
}
""", "do", "yeah:disposed!");

        verifier.Execute("""
var builder = ConsoleApp.Create();
builder.Add<MyClass>();
await builder.RunAsync(args);

public class MyClass : IDisposable
{
    public void Do()
    {
        Console.Write("yeah:");
    }

    public void Dispose()
    {
        Console.Write("disposed!");
    }
}
""", "do", "yeah:disposed!");

        verifier.Execute("""
var builder = ConsoleApp.Create();
builder.Add<MyClass>();
await builder.RunAsync(args);

public class MyClass : IAsyncDisposable
{
    public void Do()
    {
        Console.Write("yeah:");
    }

    public ValueTask DisposeAsync()
    {
        Console.Write("disposed!");
        return default;
    }
}
""", "do", "yeah:disposed!");

        // DisposeAsync: sync pattern
        verifier.Execute("""
var builder = ConsoleApp.Create();
builder.Add<MyClass>();
builder.Run(args);

public class MyClass : IAsyncDisposable
{
    public void Do()
    {
        Console.Write("yeah:");
    }

    public ValueTask DisposeAsync()
    {
        Console.Write("disposed!");
        return default;
    }
}
""", "do", "yeah:disposed!");
    }

    [Fact]
    public void ClassWithDI()
    {
        verifier.Execute("""
var serviceCollection = new MiniDI();
serviceCollection.Register(typeof(string), "hoge!");
serviceCollection.Register(typeof(int), 9999);
ConsoleApp.ServiceProvider = serviceCollection;

var builder = ConsoleApp.Create();
builder.Add<MyClass>();
builder.Run(args);

public class MyClass(string foo, int bar)
{
    public void Do()
    {
        Console.Write("yeah:");
        Console.Write(foo);
        Console.Write(bar);
    }
}

public class MiniDI : IServiceProvider
{
    System.Collections.Generic.Dictionary<Type, object> dict = new();

    public void Register(Type type, object instance)
    {
        dict[type] = instance;
    }

    public object GetService(Type serviceType)
    {
        return dict.TryGetValue(serviceType, out var instance) ? instance : null;
    }
}
""", "do", "yeah:hoge!9999");
    }

    [Fact]
    public void CommandAttr()
    {
        var code = """
var builder = ConsoleApp.Create();
builder.Add<MyClass>();
builder.Run(args);

public class MyClass()
{
    [Command("nomunomu")]
    public void Do()
    {
        Console.Write("yeah");
    }
}
""";

        verifier.Execute(code, "nomunomu", "yeah");
    }

    [Fact]
    public void CommandAttrWithFilter()
    {
        var code = """
var builder = ConsoleApp.Create();
builder.Add<MyClass>();
builder.Run(args);

public class MyClass()
{
    [ConsoleAppFilter<NopFilter1>]
    [Command("nomunomu")]
    [ConsoleAppFilter<NopFilter2>]
    public void Do()
    {
        Console.Write("command");
    }
}

internal class NopFilter1(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context,CancellationToken cancellationToken)
    {
        Console.Write("filter1-");
        return Next.InvokeAsync(context, cancellationToken);
    }
}
internal class NopFilter2(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context,CancellationToken cancellationToken)
    {
        Console.Write("filter2-");
        return Next.InvokeAsync(context, cancellationToken);
    }
}
""";

        verifier.Execute(code, "nomunomu", "filter1-filter2-command");
    }
}
