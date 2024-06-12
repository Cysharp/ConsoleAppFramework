using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppFramework.GeneratorTests;

public class BuildCustomDelegateTest(ITestOutputHelper output)
{
    VerifyHelper verifier = new VerifyHelper(output, "CAF");

    [Fact]
    public void Run()
    {
        var code = """
ConsoleApp.Run(args, (
bool a1,
bool a2,
bool a3,
bool a4,
bool a5,
bool a6,
bool a7,
bool a8,
bool a9,
bool a10,
bool a11,
bool a12,
bool a13,
bool a14,
bool a15,
bool a16 // ok it is Action
) => { Console.Write("ok"); });
""";

        verifier.Execute(code, "", "ok");

        var code2 = """
ConsoleApp.Run(args, (
bool a1,
bool a2,
bool a3,
bool a4,
bool a5,
bool a6,
bool a7,
bool a8,
bool a9,
bool a10,
bool a11,
bool a12,
bool a13,
bool a14,
bool a15,
bool a16,
bool a17 // custom delegate
) => { Console.Write("ok"); });
""";

        verifier.Execute(code2, "", "ok");


        verifier.Execute("""
var t = new Test();
ConsoleApp.Run(args, t.Handle);

public partial class Test
{
    public void Handle(
        bool a1,
        bool a2,
        bool a3,
        bool a4,
        bool a5,
        bool a6,
        bool a7,
        bool a8,
        bool a9,
        bool a10,
        bool a11,
        bool a12,
        bool a13,
        bool a14,
        bool a15,
        bool a16,
        bool a17,
        bool a18,
        bool a19
    )
    {
        Console.Write("ok");
    }
}
""", "", "ok");



        verifier.Execute("""
unsafe
{
    ConsoleApp.Run(args, &Test.Handle);
}

public partial class Test
{
    public static void Handle(
        bool a1,
        bool a2,
        bool a3,
        bool a4,
        bool a5,
        bool a6,
        bool a7,
        bool a8,
        bool a9,
        bool a10,
        bool a11,
        bool a12,
        bool a13,
        bool a14,
        bool a15,
        bool a16,
        bool a17,
        bool a18,
        bool a19
    )
    {
        Console.Write("ok");
    }
}
""", "", "ok");
    }

    [Fact]
    public void Builder()
    {
        verifier.Execute("""
var t = new Test();

var app = ConsoleApp.Create();
app.Add("", t.Handle);
app.Run(args);

public partial class Test
{
    public void Handle(
        bool a1,
        bool a2,
        bool a3,
        bool a4,
        bool a5,
        bool a6,
        bool a7,
        bool a8,
        bool a9,
        bool a10,
        bool a11,
        bool a12,
        bool a13,
        bool a14,
        bool a15,
        bool a16,
        bool a17,
        bool a18,
        bool a19
    )
    {
        Console.Write("ok");
    }
}
""", "", "ok");
    }
}
