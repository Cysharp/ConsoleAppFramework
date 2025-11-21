namespace ConsoleAppFramework.GeneratorTests;

public class DiagnosticsTest
{
    VerifyHelper verifier = new VerifyHelper("CAF");

    [Test]
    public async Task ArgumentCount()
    {
        await verifier.Verify(1, "ConsoleApp.Run(args);", "ConsoleApp.Run(args)");
        await verifier.Verify(1, "ConsoleApp.Run();", "ConsoleApp.Run()");
        await verifier.Verify(1, "ConsoleApp.Run(args, (int x, int y) => { }, 1000);", "ConsoleApp.Run(args, (int x, int y) => { }, 1000)");
    }

    [Test]
    public async Task InvalidReturnTypeFromLambda()
    {
        await verifier.Verify(2, "ConsoleApp.Run(args, string (int x, int y) => { return \"foo\"; })", "string");
        await verifier.Verify(2, "ConsoleApp.Run(args, int? (int x, int y) => { return -1; })", "int?");
        await verifier.Verify(2, "ConsoleApp.Run(args, Task (int x, int y) => { return Task.CompletedTask; })", "Task");
        await verifier.Verify(2, "ConsoleApp.Run(args, Task<int> (int x, int y) => { return Task.FromResult(0); })", "Task<int>");
        await verifier.Verify(2, "ConsoleApp.Run(args, async Task<string> (int x, int y) => { return \"foo\"; })", "Task<string>");
        await verifier.Verify(2, "ConsoleApp.Run(args, async ValueTask (int x, int y) => { })", "ValueTask");
        await verifier.Verify(2, "ConsoleApp.Run(args, async ValueTask<int> (int x, int y) => { return -1; })", "ValueTask<int>");
        await verifier.Ok("ConsoleApp.Run(args, (int x, int y) => { })");
        await verifier.Ok("ConsoleApp.Run(args, void (int x, int y) => { })");
        await verifier.Ok("ConsoleApp.Run(args, int (int x, int y) => { })");
        await verifier.Ok("ConsoleApp.Run(args, async Task (int x, int y) => { })");
        await verifier.Ok("ConsoleApp.Run(args, async Task<int> (int x, int y) => { })");
    }

    [Test]
    public async Task InvalidReturnTypeFromMethodReference()
    {
        await verifier.Verify(3, "ConsoleApp.Run(args, Invoke); float Invoke(int x, int y) => 0.3f;", "float");
        await verifier.Verify(3, "ConsoleApp.Run(args, InvokeAsync); async Task<float> InvokeAsync(int x, int y) => 0.3f;", "Task<float>");
        await verifier.Ok("ConsoleApp.Run(args, Run); void Run(int x, int y) { };");
        await verifier.Ok("ConsoleApp.Run(args, Run); static void Run(int x, int y) { };");
        await verifier.Ok("ConsoleApp.Run(args, Run); int Run(int x, int y) => -1;");
        await verifier.Ok("ConsoleApp.Run(args, Run); async Task Run(int x, int y) { };");
        await verifier.Ok("ConsoleApp.Run(args, Run); async Task<int> Run(int x, int y) => -1;");
    }

    [Test]
    public async Task RunAsyncValidation()
    {
        await verifier.Verify(2, "ConsoleApp.RunAsync(args, string (int x, int y) => { return \"foo\"; })", "string");
        await verifier.Verify(2, "ConsoleApp.RunAsync(args, int? (int x, int y) => { return -1; })", "int?");
        await verifier.Verify(2, "ConsoleApp.RunAsync(args, Task (int x, int y) => { return Task.CompletedTask; })", "Task");
        await verifier.Verify(2, "ConsoleApp.RunAsync(args, Task<int> (int x, int y) => { return Task.FromResult(0); })", "Task<int>");
        await verifier.Verify(2, "ConsoleApp.RunAsync(args, async Task<string> (int x, int y) => { return \"foo\"; })", "Task<string>");
        await verifier.Verify(2, "ConsoleApp.RunAsync(args, async ValueTask (int x, int y) => { })", "ValueTask");
        await verifier.Verify(2, "ConsoleApp.RunAsync(args, async ValueTask<int> (int x, int y) => { return -1; })", "ValueTask<int>");
        await verifier.Ok("ConsoleApp.RunAsync(args, (int x, int y) => { })");
        await verifier.Ok("ConsoleApp.RunAsync(args, void (int x, int y) => { })");
        await verifier.Ok("ConsoleApp.RunAsync(args, int (int x, int y) => { })");
        await verifier.Ok("ConsoleApp.RunAsync(args, async Task (int x, int y) => { })");
        await verifier.Ok("ConsoleApp.RunAsync(args, async Task<int> (int x, int y) => { })");

        await verifier.Verify(3, "ConsoleApp.RunAsync(args, Invoke); float Invoke(int x, int y) => 0.3f;", "float");
        await verifier.Verify(3, "ConsoleApp.RunAsync(args, InvokeAsync); async Task<float> InvokeAsync(int x, int y) => 0.3f;", "Task<float>");
        await verifier.Ok("ConsoleApp.RunAsync(args, Run); void Run(int x, int y) { };");
        await verifier.Ok("ConsoleApp.RunAsync(args, Run); static void Run(int x, int y) { };");
        await verifier.Ok("ConsoleApp.RunAsync(args, Run); int Run(int x, int y) => -1;");
        await verifier.Ok("ConsoleApp.RunAsync(args, Run); async Task Run(int x, int y) { };");
        await verifier.Ok("ConsoleApp.RunAsync(args, Run); async Task<int> Run(int x, int y) => -1;");
    }

    // v5.7.7 supports non-first argument parameters
    //[Fact]
    //public async Task Argument()
    //{
    //    await verifier.Verify(4, "ConsoleApp.Run(args, (int x, [Argument]int y) => { })", "[Argument]int y");
    //    await verifier.Verify(4, "ConsoleApp.Run(args, ([Argument]int x, int y, [Argument]int z) => { })", "[Argument]int z");
    //    await verifier.Verify(4, "ConsoleApp.Run(args, Run); void Run(int x, [Argument]int y) { };", "[Argument]int y");

    //    await verifier.Ok("ConsoleApp.Run(args, ([Argument]int x, [Argument]int y) => { })");
    //    await verifier.Ok("ConsoleApp.Run(args, Run); void Run([Argument]int x, [Argument]int y) { };");
    //}

    [Test]
    public async Task FunctionPointerValidation()
    {
        await verifier.Verify(5, "unsafe { ConsoleApp.Run(args, &Run2); static void Run2([Range(1, 10)]int x, int y) { }; }", "[Range(1, 10)]int x");

        await verifier.Ok("unsafe { ConsoleApp.Run(args, &Run2); static void Run2(int x, int y) { }; }");
    }

    [Test]
    public async Task BuilderAddConstCommandName()
    {
        await verifier.Verify(6, """
var builder = ConsoleApp.Create(); 
var baz = "foo";
builder.Add(baz, (int x, int y) => { } );
""", "baz");

        await verifier.Ok("""
var builder = ConsoleApp.Create(); 
builder.Add("foo", (int x, int y) => { } );
builder.Run(args);
""");
    }

    [Test]
    public async Task DuplicateCommandName()
    {
        await verifier.Verify(7, """
var builder = ConsoleApp.Create(); 
builder.Add("foo", (int x, int y) => { } );
builder.Add("foo", (int x, int y) => { } );
""", "\"foo\"");
    }

    [Test]
    public async Task DuplicateCommandNameClass()
    {
        await verifier.Verify(7, """
var builder = ConsoleApp.Create();
builder.Add<MyClass>();

public class MyClass
{
    public async Task Do()
    {
        Console.Write("yeah:");
    }

    public async Task Do(int i)
    {
        Console.Write("yeah:");
    }
}
""", "builder.Add<MyClass>()");

        await verifier.Verify(7, """
var builder = ConsoleApp.Create();
builder.Add("do", (int x, int y) => { } );
builder.Add<MyClass>();
builder.Run(args);

public class MyClass
{
    public async Task Do()
    {
        Console.Write("yeah:");
    }
}
""", "builder.Add<MyClass>()");
    }

    [Test]
    public async Task AddInLoop()
    {
        var myClass = """
public class MyClass
{
    public async Task Do()
    {
        Console.Write("yeah:");
    }
}
""";
        await verifier.Verify(8, $$"""
var builder = ConsoleApp.Create();
while (true)
{
    builder.Add<MyClass>();
}

{{myClass}}
""", "builder.Add<MyClass>()");

        await verifier.Verify(8, $$"""
var builder = ConsoleApp.Create();
for (int i = 0; i < 10; i++)
{
    builder.Add<MyClass>();
}

{{myClass}}
""", "builder.Add<MyClass>()");

        await verifier.Verify(8, $$"""
var builder = ConsoleApp.Create();
do
{
    builder.Add<MyClass>();
} while(true);

{{myClass}}
""", "builder.Add<MyClass>()");

        await verifier.Verify(8, $$"""
var builder = ConsoleApp.Create();
foreach (var item in new[]{1,2,3})
{
    builder.Add<MyClass>();
}

{{myClass}}
""", "builder.Add<MyClass>()");
    }

    [Test]
    public async Task ErrorInBuilderAPI()
    {
        await verifier.Verify(3, $$"""
var builder = ConsoleApp.Create();
builder.Add<MyClass>();

public class MyClass
{
    public string Do()
    {
        Console.Write("yeah:");
        return "foo";
    }
}
""", "string");

        await verifier.Verify(3, $$"""
var builder = ConsoleApp.Create();
builder.Add<MyClass>();

public class MyClass
{
    public async Task<string> Do()
    {
        Console.Write("yeah:");
        return "foo";
    }
}
""", "Task<string>");

        await verifier.Verify(2, $$"""
var builder = ConsoleApp.Create();
builder.Add("foo", string (int x, int y) => { return "foo"; });
""", "string");

        await verifier.Verify(2, $$"""
var builder = ConsoleApp.Create();
builder.Add("foo", async Task<string> (int x, int y) => { return "foo"; });
""", "Task<string>");
    }



    [Test]
    public async Task RunAndFilter()
    {
        await verifier.Verify(9, """
ConsoleApp.Run(args, Hello);

[ConsoleAppFilter<NopFilter>]
void Hello()
{
}

public class NopFilter(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(CancellationToken cancellationToken)
    {
        return Next.InvokeAsync(cancellationToken);
    }
}
""", "ConsoleApp.Run(args, Hello)");
    }

    [Test]
    public async Task MultiConstructorFilter()
    {
        await verifier.Verify(10, """
var app = ConsoleApp.Create();
app.UseFilter<NopFilter>();
app.Add("", Hello);
app.Run(args);

void Hello()
{
}

internal class NopFilter : ConsoleAppFilter
{
    public NopFilter(ConsoleAppFilter next)
        :base(next)
    {
    }

    public NopFilter(string x, ConsoleAppFilter next)
        :base(next)
    {
    }

    public override Task InvokeAsync(CancellationToken cancellationToken)
    {
        return Next.InvokeAsync(cancellationToken);
    }
}
""", "NopFilter");

        await verifier.Verify(10, """
var app = ConsoleApp.Create();
app.Add<Foo>();
app.Run(args);

[ConsoleAppFilter<NopFilter>]
public class Foo
{
    public async Task Hello()
    {
    }
}

internal class NopFilter : ConsoleAppFilter
{
    public NopFilter(ConsoleAppFilter next)
        :base(next)
    {
    }

    public NopFilter(string x, ConsoleAppFilter next)
        :base(next)
    {
    }

    public override Task InvokeAsync(CancellationToken cancellationToken)
    {
        return Next.InvokeAsync(cancellationToken);
    }
}
""", "ConsoleAppFilter<NopFilter>");

        await verifier.Verify(10, """
var app = ConsoleApp.Create();
app.Add<Foo>();
app.Run(args);

public class Foo
{
    [ConsoleAppFilter<NopFilter>]
    public async Task Hello()
    {
    }
}

internal class NopFilter : ConsoleAppFilter
{
    public NopFilter(ConsoleAppFilter next)
        :base(next)
    {
    }

    public NopFilter(string x, ConsoleAppFilter next)
        :base(next)
    {
    }

    public override Task InvokeAsync(CancellationToken cancellationToken)
    {
        return Next.InvokeAsync(cancellationToken);
    }
}
""", "ConsoleAppFilter<NopFilter>");
    }


    [Test]
    public async Task MultipleCtorClass()
    {
        await verifier.Verify(11, """
var app = ConsoleApp.Create();
app.Add<Foo>();
app.Run(args);

public class Foo
{
    public Foo() { }
    public Foo(int x) { }

    public async Task Hello()
    {
    }
}
""", "app.Add<Foo>()");
    }

    [Test]
    public async Task PublicMethods()
    {
        await verifier.Verify(12, """
var app = ConsoleApp.Create();
app.Add<Foo>();
app.Run(args);

public class Foo
{
    public Foo() { }
    public Foo(int x) { }

    private void Hello()
    {
    }
}
""", "app.Add<Foo>()");
    }

    [Test]
    public async Task AbstractNotAllow()
    {
        await verifier.Verify(13, """
var app = ConsoleApp.Create();
app.Add<Foo>();
app.Run(args);

public abstract class Foo
{
    public async Task Hello()
    {
    }
}
""", "app.Add<Foo>()");

        await verifier.Verify(13, """
var app = ConsoleApp.Create();
app.Add<IFoo>();
app.Run(args);

public interface IFoo
{
    void Hello();
}
""", "app.Add<IFoo>()");
    }

    [Test]
    public async Task DocCommentName()
    {
        await verifier.Verify(15, """
var app = ConsoleApp.Create();
app.Add<Foo>();
app.Run(args);

public class Foo
{
    /// <param name="nomsg">foobarbaz!</param>
    [Command("Error1")]
    public async Task Bar(string msg)
    {
        Console.WriteLine(msg);
    }
}

""", "Bar");

    }

    [Test]
    public async Task AsyncVoid()
    {
        await verifier.Verify(16, """
var app = ConsoleApp.Create();
app.Add<MyCommands2>();
app.Run(args);

public class MyCommands2
{
    public async void Foo()
    {
        await Task.Yield();
    }
}

""", "async");
    }

    [Test]
    public async Task GlobalOptionsDuplicate()
    {
        await verifier.Verify(17, """
var app = ConsoleApp.Create();

app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    return new object();
});

app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    return new object();
});

app.Run(args);
""", "app.ConfigureGlobalOptions");
    }

    [Test]
    public async Task GlobalOptionsInvalidType()
    {
        await verifier.Verify(18, """
var app = ConsoleApp.Create();

app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    builder.AddGlobalOption<System.Version>("foo");
    return new object();
});

app.Run(args);
""", "builder.AddGlobalOption<System.Version>(\"foo\")");
    }

}
