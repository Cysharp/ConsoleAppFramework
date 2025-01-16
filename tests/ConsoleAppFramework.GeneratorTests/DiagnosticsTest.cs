using Xunit.Abstractions;

namespace ConsoleAppFramework.GeneratorTests;

public class DiagnosticsTest(ITestOutputHelper output)
{
    VerifyHelper verifier = new VerifyHelper(output, "CAF");

    [Fact]
    public void ArgumentCount()
    {
        verifier.Verify(1, "ConsoleApp.Run(args);", "ConsoleApp.Run(args)");
        verifier.Verify(1, "ConsoleApp.Run();", "ConsoleApp.Run()");
        verifier.Verify(1, "ConsoleApp.Run(args, (int x, int y) => { }, 1000);", "ConsoleApp.Run(args, (int x, int y) => { }, 1000)");
    }

    [Fact]
    public void InvalidReturnTypeFromLambda()
    {
        verifier.Verify(2, "ConsoleApp.Run(args, string (int x, int y) => { return \"foo\"; })", "string");
        verifier.Verify(2, "ConsoleApp.Run(args, int? (int x, int y) => { return -1; })", "int?");
        verifier.Verify(2, "ConsoleApp.Run(args, Task (int x, int y) => { return Task.CompletedTask; })", "Task");
        verifier.Verify(2, "ConsoleApp.Run(args, Task<int> (int x, int y) => { return Task.FromResult(0); })", "Task<int>");
        verifier.Verify(2, "ConsoleApp.Run(args, async Task<string> (int x, int y) => { return \"foo\"; })", "Task<string>");
        verifier.Verify(2, "ConsoleApp.Run(args, async ValueTask (int x, int y) => { })", "ValueTask");
        verifier.Verify(2, "ConsoleApp.Run(args, async ValueTask<int> (int x, int y) => { return -1; })", "ValueTask<int>");
        verifier.Ok("ConsoleApp.Run(args, (int x, int y) => { })");
        verifier.Ok("ConsoleApp.Run(args, void (int x, int y) => { })");
        verifier.Ok("ConsoleApp.Run(args, int (int x, int y) => { })");
        verifier.Ok("ConsoleApp.Run(args, async Task (int x, int y) => { })");
        verifier.Ok("ConsoleApp.Run(args, async Task<int> (int x, int y) => { })");
    }

    [Fact]
    public void InvalidReturnTypeFromMethodReference()
    {
        verifier.Verify(3, "ConsoleApp.Run(args, Invoke); float Invoke(int x, int y) => 0.3f;", "float");
        verifier.Verify(3, "ConsoleApp.Run(args, InvokeAsync); async Task<float> InvokeAsync(int x, int y) => 0.3f;", "Task<float>");
        verifier.Ok("ConsoleApp.Run(args, Run); void Run(int x, int y) { };");
        verifier.Ok("ConsoleApp.Run(args, Run); static void Run(int x, int y) { };");
        verifier.Ok("ConsoleApp.Run(args, Run); int Run(int x, int y) => -1;");
        verifier.Ok("ConsoleApp.Run(args, Run); async Task Run(int x, int y) { };");
        verifier.Ok("ConsoleApp.Run(args, Run); async Task<int> Run(int x, int y) => -1;");
    }

    [Fact]
    public void RunAsyncValidation()
    {
        verifier.Verify(2, "ConsoleApp.RunAsync(args, string (int x, int y) => { return \"foo\"; })", "string");
        verifier.Verify(2, "ConsoleApp.RunAsync(args, int? (int x, int y) => { return -1; })", "int?");
        verifier.Verify(2, "ConsoleApp.RunAsync(args, Task (int x, int y) => { return Task.CompletedTask; })", "Task");
        verifier.Verify(2, "ConsoleApp.RunAsync(args, Task<int> (int x, int y) => { return Task.FromResult(0); })", "Task<int>");
        verifier.Verify(2, "ConsoleApp.RunAsync(args, async Task<string> (int x, int y) => { return \"foo\"; })", "Task<string>");
        verifier.Verify(2, "ConsoleApp.RunAsync(args, async ValueTask (int x, int y) => { })", "ValueTask");
        verifier.Verify(2, "ConsoleApp.RunAsync(args, async ValueTask<int> (int x, int y) => { return -1; })", "ValueTask<int>");
        verifier.Ok("ConsoleApp.RunAsync(args, (int x, int y) => { })");
        verifier.Ok("ConsoleApp.RunAsync(args, void (int x, int y) => { })");
        verifier.Ok("ConsoleApp.RunAsync(args, int (int x, int y) => { })");
        verifier.Ok("ConsoleApp.RunAsync(args, async Task (int x, int y) => { })");
        verifier.Ok("ConsoleApp.RunAsync(args, async Task<int> (int x, int y) => { })");

        verifier.Verify(3, "ConsoleApp.RunAsync(args, Invoke); float Invoke(int x, int y) => 0.3f;", "float");
        verifier.Verify(3, "ConsoleApp.RunAsync(args, InvokeAsync); async Task<float> InvokeAsync(int x, int y) => 0.3f;", "Task<float>");
        verifier.Ok("ConsoleApp.RunAsync(args, Run); void Run(int x, int y) { };");
        verifier.Ok("ConsoleApp.RunAsync(args, Run); static void Run(int x, int y) { };");
        verifier.Ok("ConsoleApp.RunAsync(args, Run); int Run(int x, int y) => -1;");
        verifier.Ok("ConsoleApp.RunAsync(args, Run); async Task Run(int x, int y) { };");
        verifier.Ok("ConsoleApp.RunAsync(args, Run); async Task<int> Run(int x, int y) => -1;");
    }

    [Fact]
    public void Argument()
    {
        verifier.Verify(4, "ConsoleApp.Run(args, (int x, [Argument]int y) => { })", "[Argument]int y");
        verifier.Verify(4, "ConsoleApp.Run(args, ([Argument]int x, int y, [Argument]int z) => { })", "[Argument]int z");
        verifier.Verify(4, "ConsoleApp.Run(args, Run); void Run(int x, [Argument]int y) { };", "[Argument]int y");

        verifier.Ok("ConsoleApp.Run(args, ([Argument]int x, [Argument]int y) => { })");
        verifier.Ok("ConsoleApp.Run(args, Run); void Run([Argument]int x, [Argument]int y) { };");
    }

    [Fact]
    public void FunctionPointerValidation()
    {
        verifier.Verify(5, "unsafe { ConsoleApp.Run(args, &Run2); static void Run2([Range(1, 10)]int x, int y) { }; }", "[Range(1, 10)]int x");

        verifier.Ok("unsafe { ConsoleApp.Run(args, &Run2); static void Run2(int x, int y) { }; }");
    }

    [Fact]
    public void BuilderAddConstCommandName()
    {
        verifier.Verify(6, """
var builder = ConsoleApp.Create(); 
var baz = "foo";
builder.Add(baz, (int x, int y) => { } );
""", "baz");

        verifier.Ok("""
var builder = ConsoleApp.Create(); 
builder.Add("foo", (int x, int y) => { } );
builder.Run(args);
""");
    }

    [Fact]
    public void DuplicateCommandName()
    {
        verifier.Verify(7, """
var builder = ConsoleApp.Create(); 
builder.Add("foo", (int x, int y) => { } );
builder.Add("foo", (int x, int y) => { } );
""", "\"foo\"");
    }

    [Fact]
    public void DuplicateCommandNameClass()
    {
        verifier.Verify(7, """
var builder = ConsoleApp.Create();
builder.Add<MyClass>();

public class MyClass
{
    public void Do()
    {
        Console.Write("yeah:");
    }

    public void Do(int i)
    {
        Console.Write("yeah:");
    }
}
""", "builder.Add<MyClass>()");

        verifier.Verify(7, """
var builder = ConsoleApp.Create();
builder.Add("do", (int x, int y) => { } );
builder.Add<MyClass>();
builder.Run(args);

public class MyClass
{
    public void Do()
    {
        Console.Write("yeah:");
    }
}
""", "builder.Add<MyClass>()");
    }

    [Fact]
    public void AddInLoop()
    {
        var myClass = """
public class MyClass
{
    public void Do()
    {
        Console.Write("yeah:");
    }
}
""";
        verifier.Verify(8, $$"""
var builder = ConsoleApp.Create();
while (true)
{
    builder.Add<MyClass>();
}

{{myClass}}
""", "builder.Add<MyClass>()");

        verifier.Verify(8, $$"""
var builder = ConsoleApp.Create();
for (int i = 0; i < 10; i++)
{
    builder.Add<MyClass>();
}

{{myClass}}
""", "builder.Add<MyClass>()");

        verifier.Verify(8, $$"""
var builder = ConsoleApp.Create();
do
{
    builder.Add<MyClass>();
} while(true);

{{myClass}}
""", "builder.Add<MyClass>()");

        verifier.Verify(8, $$"""
var builder = ConsoleApp.Create();
foreach (var item in new[]{1,2,3})
{
    builder.Add<MyClass>();
}

{{myClass}}
""", "builder.Add<MyClass>()");
    }

    [Fact]
    public void ErrorInBuilderAPI()
    {
        verifier.Verify(3, $$"""
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

        verifier.Verify(3, $$"""
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

        verifier.Verify(2, $$"""
var builder = ConsoleApp.Create();
builder.Add("foo", string (int x, int y) => { return "foo"; });
""", "string");

        verifier.Verify(2, $$"""
var builder = ConsoleApp.Create();
builder.Add("foo", async Task<string> (int x, int y) => { return "foo"; });
""", "Task<string>");
    }



    [Fact]
    public void RunAndFilter()
    {
        verifier.Verify(9, """
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

    [Fact]
    public void MultiConstructorFilter()
    {
        verifier.Verify(10, """
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

        verifier.Verify(10, """
var app = ConsoleApp.Create();
app.Add<Foo>();
app.Run(args);

[ConsoleAppFilter<NopFilter>]
public class Foo
{
    public void Hello()
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

        verifier.Verify(10, """
var app = ConsoleApp.Create();
app.Add<Foo>();
app.Run(args);

public class Foo
{
    [ConsoleAppFilter<NopFilter>]
    public void Hello()
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


    [Fact]
    public void MultipleCtorClass()
    {
        verifier.Verify(11, """
var app = ConsoleApp.Create();
app.Add<Foo>();
app.Run(args);

public class Foo
{
    public Foo() { }
    public Foo(int x) { }

    public void Hello()
    {
    }
}
""", "app.Add<Foo>()");
    }

    [Fact]
    public void PublicMethods()
    {
        verifier.Verify(12, """
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

    [Fact]
    public void AbstractNotAllow()
    {
        verifier.Verify(13, """
var app = ConsoleApp.Create();
app.Add<Foo>();
app.Run(args);

public abstract class Foo
{
    public void Hello()
    {
    }
}
""", "app.Add<Foo>()");

        verifier.Verify(13, """
var app = ConsoleApp.Create();
app.Add<IFoo>();
app.Run(args);

public interface IFoo
{
    void Hello();
}
""", "app.Add<IFoo>()");
    }

    [Fact]
    public void DocCommentName()
    {
        verifier.Verify(15, """
var app = ConsoleApp.Create();
app.Add<Foo>();
app.Run(args);

public class Foo
{
    /// <param name="nomsg">foobarbaz!</param>
    [Command("Error1")]
    public void Bar(string msg)
    {
        Console.WriteLine(msg);
    }
}

""", "Bar");

    }

    [Fact]
    public void AsyncVoid()
    {
        verifier.Verify(16, """
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
}

