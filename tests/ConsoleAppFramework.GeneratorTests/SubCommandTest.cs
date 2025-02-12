using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace ConsoleAppFramework.GeneratorTests;

public class SubCommandTest(ITestOutputHelper output)
{
    VerifyHelper verifier = new VerifyHelper(output, "CAF");

    [Fact]
    public void Zeroargs()
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

        verifier.Execute(code, "", "root");
        verifier.Execute(code, "a", "a");
        verifier.Execute(code, "a b1", "a b1");
        verifier.Execute(code, "a b2", "a b2");
        verifier.Execute(code, "a b2 c", "a b2 c");
        verifier.Execute(code, "a b2 d", "a b2 d");
        verifier.Execute(code, "a b2 d e", "a b2 d e");
        verifier.Execute(code, "a b c d e f", "a b c d e f");
    }

    [Fact]
    public void Withargs()
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

        verifier.Execute(code, "--x 10 --y 20", "root 10 20");
        verifier.Execute(code, "a --x 10 --y 20", "a 10 20");
        verifier.Execute(code, "a b1 --x 10 --y 20", "a b1 10 20");
        verifier.Execute(code, "a b2 --x 10 --y 20", "a b2 10 20");
        verifier.Execute(code, "a b2 c --x 10 --y 20", "a b2 c 10 20");
        verifier.Execute(code, "a b2 d --x 10 --y 20", "a b2 d 10 20");
        verifier.Execute(code, "a b2 d e --x 10 --y 20", "a b2 d e 10 20");
        verifier.Execute(code, "a b c d e f --x 10 --y 20", "a b c d e f 10 20");
    }

    [Fact]
    public void ZeroargsAsync()
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

        verifier.Execute(code, "", "root");
        verifier.Execute(code, "a", "a");
        verifier.Execute(code, "a b1", "a b1");
        verifier.Execute(code, "a b2", "a b2");
        verifier.Execute(code, "a b2 c", "a b2 c");
        verifier.Execute(code, "a b2 d", "a b2 d");
        verifier.Execute(code, "a b2 d e", "a b2 d e");
        verifier.Execute(code, "a b c d e f", "a b c d e f");
    }

    [Fact]
    public void WithargsAsync()
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

        verifier.Execute(code, "--x 10 --y 20", "root 10 20");
        verifier.Execute(code, "a --x 10 --y 20", "a 10 20");
        verifier.Execute(code, "a b1 --x 10 --y 20", "a b1 10 20");
        verifier.Execute(code, "a b2 --x 10 --y 20", "a b2 10 20");
        verifier.Execute(code, "a b2 c --x 10 --y 20", "a b2 c 10 20");
        verifier.Execute(code, "a b2 d --x 10 --y 20", "a b2 d 10 20");
        verifier.Execute(code, "a b2 d e --x 10 --y 20", "a b2 d e 10 20");
        verifier.Execute(code, "a b c d e f --x 10 --y 20", "a b c d e f 10 20");
    }
}
