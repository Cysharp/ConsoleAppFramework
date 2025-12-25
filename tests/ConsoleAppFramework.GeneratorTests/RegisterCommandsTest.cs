using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFramework.GeneratorTests;

[ClassDataSource<VerifyHelper>]
public class RegisterCommandsTest(VerifyHelper verifier)
{
    [Test]
    public async Task VerifyDuplicate()
    {
        await verifier.Verify(7, """
var app = ConsoleApp.Create();
app.Run(args);

[RegisterCommands]
public class Foo
{
    public async Task Bar(int x)
    {
        Console.Write(x);
    }

    public async Task Baz(int y)
    {
        Console.Write(y);
    }
}

[RegisterCommands]
public class Hoge
{
    public async Task Bar(int x)
    {
        Console.Write(x);
    }

    public async Task Baz(int y)
    {
        Console.Write(y);
    }
}
""", "Bar");
    }

    [Test]
    public async Task Exec()
    {
        var code = """
var app = ConsoleApp.Create();
app.Run(args);

[RegisterCommands]
public class Foo
{
    public async Task Bar(int x)
    {
        Console.Write(x);
    }

    public async Task Baz(int y)
    {
        Console.Write(y);
    }
}

[RegisterCommands("hoge")]
public class Hoge
{
    public async Task Bar(int x)
    {
        Console.Write(x);
    }

    public async Task Baz(int y)
    {
        Console.Write(y);
    }
}
""";

        await verifier.Execute(code, "bar --x 10", "10");
        await verifier.Execute(code, "baz --y 20", "20");
        await verifier.Execute(code, "hoge bar --x 10", "10");
        await verifier.Execute(code, "hoge baz --y 20", "20");
    }
}
