namespace ConsoleAppFramework.GeneratorTests;

public class SubCommandTest
{
    VerifyHelper verifier = new VerifyHelper("CAF");

    [Test]
    public async Task Zeroargs()
    {
        var code = """
var builder = ConsoleApp.Create();
            
builder.Add("", () => { Console.Write("root"); });
builder.Add("a", () => { Console.Write("a"); });
builder.Add("a b1", () => { Console.Write("a b1"); });
builder.Add("a b2", () => { Console.Write("a b2"); });
builder.Add("a b2 c", () => { Console.Write("a b2 c"); });
builder.Add("a b2 d", () => { Console.Write("a b2 d"); });
builder.Add("a b2 d e", () => { Console.Write("a b2 d e"); });
builder.Add("a b c d e f", () => { Console.Write("a b c d e f"); });
            
builder.Run(args);
""";

        await verifier.Execute(code, "", "root");
        await verifier.Execute(code, "a", "a");
        await verifier.Execute(code, "a b1", "a b1");
        await verifier.Execute(code, "a b2", "a b2");
        await verifier.Execute(code, "a b2 c", "a b2 c");
        await verifier.Execute(code, "a b2 d", "a b2 d");
        await verifier.Execute(code, "a b2 d e", "a b2 d e");
        await verifier.Execute(code, "a b c d e f", "a b c d e f");
    }

    [Test]
    public async Task Withargs()
    {
        var code = """
var builder = ConsoleApp.Create();
            
builder.Add("", (int x, int y) => { Console.Write($"root {x} {y}"); });
builder.Add("a", (int x, int y) => { Console.Write($"a {x} {y}"); });
builder.Add("a b1", (int x, int y) => { Console.Write($"a b1 {x} {y}"); });
builder.Add("a b2", (int x, int y) => { Console.Write($"a b2 {x} {y}"); });
builder.Add("a b2 c", (int x, int y) => { Console.Write($"a b2 c {x} {y}"); });
builder.Add("a b2 d", (int x, int y) => { Console.Write($"a b2 d {x} {y}"); });
builder.Add("a b2 d e", (int x, int y) => { Console.Write($"a b2 d e {x} {y}"); });
builder.Add("a b c d e f", (int x, int y) => { Console.Write($"a b c d e f {x} {y}"); });

builder.Run(args);
""";

        await verifier.Execute(code, "--x 10 --y 20", "root 10 20");
        await verifier.Execute(code, "a --x 10 --y 20", "a 10 20");
        await verifier.Execute(code, "a b1 --x 10 --y 20", "a b1 10 20");
        await verifier.Execute(code, "a b2 --x 10 --y 20", "a b2 10 20");
        await verifier.Execute(code, "a b2 c --x 10 --y 20", "a b2 c 10 20");
        await verifier.Execute(code, "a b2 d --x 10 --y 20", "a b2 d 10 20");
        await verifier.Execute(code, "a b2 d e --x 10 --y 20", "a b2 d e 10 20");
        await verifier.Execute(code, "a b c d e f --x 10 --y 20", "a b c d e f 10 20");
    }

    [Test]
    public async Task ZeroargsAsync()
    {
        var code = """
var builder = ConsoleApp.Create();
            
builder.Add("", () => { Console.Write("root"); });
builder.Add("a", () => { Console.Write("a"); });
builder.Add("a b1", () => { Console.Write("a b1"); });
builder.Add("a b2", () => { Console.Write("a b2"); });
builder.Add("a b2 c", () => { Console.Write("a b2 c"); });
builder.Add("a b2 d", () => { Console.Write("a b2 d"); });
builder.Add("a b2 d e", () => { Console.Write("a b2 d e"); });
builder.Add("a b c d e f", () => { Console.Write("a b c d e f"); });
            
await builder.RunAsync(args);
""";

        await verifier.Execute(code, "", "root");
        await verifier.Execute(code, "a", "a");
        await verifier.Execute(code, "a b1", "a b1");
        await verifier.Execute(code, "a b2", "a b2");
        await verifier.Execute(code, "a b2 c", "a b2 c");
        await verifier.Execute(code, "a b2 d", "a b2 d");
        await verifier.Execute(code, "a b2 d e", "a b2 d e");
        await verifier.Execute(code, "a b c d e f", "a b c d e f");
    }

    [Test]
    public async Task WithargsAsync()
    {
        var code = """
var builder = ConsoleApp.Create();
            
builder.Add("", (int x, int y) => { Console.Write($"root {x} {y}"); });
builder.Add("a", (int x, int y) => { Console.Write($"a {x} {y}"); });
builder.Add("a b1", (int x, int y) => { Console.Write($"a b1 {x} {y}"); });
builder.Add("a b2", (int x, int y) => { Console.Write($"a b2 {x} {y}"); });
builder.Add("a b2 c", (int x, int y) => { Console.Write($"a b2 c {x} {y}"); });
builder.Add("a b2 d", (int x, int y) => { Console.Write($"a b2 d {x} {y}"); });
builder.Add("a b2 d e", (int x, int y) => { Console.Write($"a b2 d e {x} {y}"); });
builder.Add("a b c d e f", (int x, int y) => { Console.Write($"a b c d e f {x} {y}"); });

await builder.RunAsync(args);
""";

        await verifier.Execute(code, "--x 10 --y 20", "root 10 20");
        await verifier.Execute(code, "a --x 10 --y 20", "a 10 20");
        await verifier.Execute(code, "a b1 --x 10 --y 20", "a b1 10 20");
        await verifier.Execute(code, "a b2 --x 10 --y 20", "a b2 10 20");
        await verifier.Execute(code, "a b2 c --x 10 --y 20", "a b2 c 10 20");
        await verifier.Execute(code, "a b2 d --x 10 --y 20", "a b2 d 10 20");
        await verifier.Execute(code, "a b2 d e --x 10 --y 20", "a b2 d e 10 20");
        await verifier.Execute(code, "a b c d e f --x 10 --y 20", "a b c d e f 10 20");
    }
}
