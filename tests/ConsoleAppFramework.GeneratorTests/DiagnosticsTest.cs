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
        verifier.Verify(3, "ConsoleApp.Run(args, Invoke); float Invoke(int x, int y) => 0.3f;", "Invoke");
        verifier.Verify(3, "ConsoleApp.Run(args, InvokeAsync); async Task<float> InvokeAsync(int x, int y) => 0.3f;", "InvokeAsync");
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

        verifier.Verify(3, "ConsoleApp.RunAsync(args, Invoke); float Invoke(int x, int y) => 0.3f;", "Invoke");
        verifier.Verify(3, "ConsoleApp.RunAsync(args, InvokeAsync); async Task<float> InvokeAsync(int x, int y) => 0.3f;", "InvokeAsync");
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
    public void BuilderAddConst()
    {
        verifier.Ok("""
var builder = ConsoleApp.CreateBuilder(); 
builder.Add("foo", (int x, int y) => { } );
builder.Run(args);
""");



    }
}
