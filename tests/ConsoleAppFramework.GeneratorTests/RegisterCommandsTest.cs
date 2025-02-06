using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFramework.GeneratorTests;

public class RegisterCommandsTest(ITestOutputHelper output)
{
    readonly VerifyHelper verifier = new(output, "CAF");

    [Fact]
    public void VerifyDuplicate()
    {
        verifier.Verify(7, """
var app = ConsoleApp.Create();
app.Run(args);

[RegisterCommands]
public class Foo
{
    public void Bar(int x)
    {
        Console.Write(x);
    }

    public void Baz(int y)
    {
        Console.Write(y);
    }
}

[RegisterCommands]
public class Hoge
{
    public void Bar(int x)
    {
        Console.Write(x);
    }

    public void Baz(int y)
    {
        Console.Write(y);
    }
}
""", "Bar");
    }

    [Fact]
    public void Exec()
    {
        var code = """
var app = ConsoleApp.Create();
app.Run(args);

[RegisterCommands]
public class Foo
{
    public void Bar(int x)
    {
        Console.Write(x);
    }

    public void Baz(int y)
    {
        Console.Write(y);
    }
}

[RegisterCommands("hoge")]
public class Hoge
{
    public void Bar(int x)
    {
        Console.Write(x);
    }

    public void Baz(int y)
    {
        Console.Write(y);
    }
}
""";

        verifier.Execute(code, "bar --x 10", "10");
        verifier.Execute(code, "baz --y 20", "20");
        verifier.Execute(code, "hoge bar --x 10", "10");
        verifier.Execute(code, "hoge baz --y 20", "20");
    }
}
