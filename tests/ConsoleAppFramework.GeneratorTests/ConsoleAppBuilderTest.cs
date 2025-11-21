namespace ConsoleAppFramework.GeneratorTests;

public class ConsoleAppBuilderTest : IDisposable
{
    VerifyHelper verifier = new VerifyHelper("CAF");

    public void Dispose() => Environment.ExitCode = 0;

    [Test]
    public async Task BuilderRun()
    {
        var code = """
var builder = ConsoleApp.Create();
builder.Add("foo", (int x, int y) => { Console.Write(x + y); });
builder.Add("bar", (int x, int y = 10) => { Console.Write(x + y); });
builder.Add("baz", int (int x, string y) => { Console.Write(x + y); return 10; });
builder.Add("boz", async Task (int x) => { await Task.Yield(); Console.Write(x * 2); });
builder.Run(args);
""";
        await verifier.Execute(code, "foo --x 10 --y 20", "30");
        await verifier.Execute(code, "bar --x 20 --y 30", "50");
        await verifier.Execute(code, "bar --x 20", "30");
        await Assert.That(Environment.ExitCode).IsZero();
        await verifier.Execute(code, "baz --x 40 --y takoyaki", "40takoyaki");
        await Assert.That(Environment.ExitCode).IsEqualTo(10);
        Environment.ExitCode = 0;

        await verifier.Execute(code, "boz --x 40", "80");
    }

    [Test]
    public async Task BuilderRunAsync()
    {
        var code = """
var builder = ConsoleApp.Create();
builder.Add("foo", (int x, int y) => { Console.Write(x + y); });
builder.Add("bar", (int x, int y = 10) => { Console.Write(x + y); });
builder.Add("baz", int (int x, string y) => { Console.Write(x + y); return 10; });
builder.Add("boz", async Task (int x) => { await Task.Yield(); Console.Write(x * 2); });
await builder.RunAsync(args);
""";

        await verifier.Execute(code, "foo --x 10 --y 20", "30");
        await verifier.Execute(code, "bar --x 20 --y 30", "50");
        await verifier.Execute(code, "bar --x 20", "30");
        await Assert.That(Environment.ExitCode).IsZero();
        await verifier.Execute(code, "baz --x 40 --y takoyaki", "40takoyaki");
        await Assert.That(Environment.ExitCode).IsEqualTo(10);
        Environment.ExitCode = 0;

        await verifier.Execute(code, "boz --x 40", "80");
    }

    [Test]
    public async Task AddClass()
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

        await verifier.Execute(code, "do", "yeah");
        await verifier.Execute(code, "sum --x 1 --y 2", "3");
        await verifier.Execute(code, "echo --msg takoyaki", "takoyaki");
    }

    [Test]
    public async Task ClassDispose()
    {
        await verifier.Execute("""
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

        await verifier.Execute("""
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

        await verifier.Execute("""
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
        await verifier.Execute("""
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

    [Test]
    public async Task ClassWithDI()
    {
        await verifier.Execute("""
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

    [Test]
    public async Task CommandAttr()
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

        await verifier.Execute(code, "nomunomu", "yeah");
    }

    [Test]
    public async Task CommandAttrWithFilter()
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

        await verifier.Execute(code, "nomunomu", "filter1-filter2-command");
    }

    [Test]
    public async Task CommandAlias()
    {
        var code = """
var app = ConsoleApp.Create();

app.Add("build|b", () => { Console.Write("build ok"); });
app.Add("test|t", () => { Console.Write("test ok");  });
app.Add<Commands>();

app.Run(args);

public class Commands
{
    /// <summary>Analyze the current package and report errors, but don't build object files.</summary>
    [Command("check|c")]
    public void Check() { Console.Write("check ok"); }

    /// <summary>Build this packages's and its dependencies' documenation.</summary>
    [Command("doc|d")]
    public void Doc() { Console.Write("doc ok"); }
}
""";

        await verifier.Execute(code, "b", "build ok");
        await verifier.Execute(code, "build", "build ok");
        await verifier.Execute(code, "t", "test ok");
        await verifier.Execute(code, "test", "test ok");
        await verifier.Execute(code, "c", "check ok");
        await verifier.Execute(code, "check", "check ok");
        await verifier.Execute(code, "d", "doc ok");
        await verifier.Execute(code, "doc", "doc ok");
    }
}
