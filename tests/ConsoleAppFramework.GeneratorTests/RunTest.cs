namespace ConsoleAppFramework.GeneratorTests;

public class Test(ITestOutputHelper output) : IDisposable
{
    VerifyHelper verifier = new VerifyHelper(output, "CAF");

    public void Dispose() => Environment.ExitCode = 0;

    [Fact]
    public void SyncRun()
    {
        verifier.Execute("ConsoleApp.Run(args, (int x, int y) => { Console.Write((x + y)); });", "--x 10 --y 20", "30");
    }

    [Fact]
    public void SyncRunShouldFailed()
    {
        verifier.Error("ConsoleApp.Run(args, (int x) => { Console.Write((x)); });", "--x").ShouldContain("Argument 'x' failed to parse");
    }

    [Fact]
    public void MissingArgument()
    {
        verifier.Error("ConsoleApp.Run(args, (int x, int y) => { Console.Write((x + y)); });", "--x 10 y 20").ShouldContain("Argument 'y' is not recognized.");

        Environment.ExitCode.ShouldBe(1);
        Environment.ExitCode = 0;
    }

    [Fact]
    public void ValidateOne()
    {
        var expected = """
The field x must be between 1 and 10.


""";

        verifier.Execute("""
ConsoleApp.Run(args, ([Range(1, 10)]int x, [Range(100, 200)]int y) => { Console.Write((x + y)); });
""", "--x 100 --y 140", expected);

        Environment.ExitCode.ShouldBe(1);
        Environment.ExitCode = 0;
    }

    [Fact]
    public void ValidateTwo()
    {
        var expected = """
The field x must be between 1 and 10.
The field y must be between 100 and 200.


""";

        verifier.Execute("""
ConsoleApp.Run(args, ([Range(1, 10)]int x, [Range(100, 200)]int y) => { Console.Write((x + y)); });
""", "--x 100 --y 240", expected);

        Environment.ExitCode.ShouldBe(1);
        Environment.ExitCode = 0;
    }
    [Fact]
    public void Parameters()
    {
        verifier.Execute("""
ConsoleApp.Run(args, (int foo, string bar, Fruit ft, bool flag, Half half, int? itt, Takoyaki.Obj obj) => 
{
    Console.Write(foo); 
    Console.Write(bar); 
    Console.Write(ft); 
    Console.Write(flag); 
    Console.Write(half); 
    Console.Write(itt);
    Console.Write(obj.Foo); 
});

enum Fruit
{
    Orange, Grape, Apple
}

namespace Takoyaki
{
    public class Obj
    {
         public int Foo { get; set; }
    }
}
""", "--foo 10 --bar aiueo --ft Grape --flag --half 1.3 --itt 99 --obj {\"Foo\":1999}", "10aiueoGrapeTrue1.3991999");
    }

    [Fact]
    public void ValidateClass()
    {
        var expected = """
The field value must be between 0 and 1.


""";

        verifier.Execute("""
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test
{
    public void Show(string aaa, [Range(0, 1)] double value) => ConsoleApp.Log($"{value}");
}

""", "show --aaa foo --value 100", expected);

    }

    [Fact]
    public void StringEscape()
    {
        var code = """
var app = ConsoleApp.Create();
app.Add<MyCommands>();
app.Run(args);

public class MyCommands
{
    [Command("Error1")]
    public void Error1(string msg = @"\")
    {
        Console.Write(msg);
    }
    [Command("Error2")]
    public void Error2(string msg = "\\")
    {
        Console.Write(msg);
    }
    [Command("Output")]
    public void Output(string msg = @"\\")
    {
        Console.Write(msg); 
    }
}
""";

        verifier.Execute(code, "Error1", @"\");
        verifier.Execute(code, "Error2", "\\");
        verifier.Execute(code, "Output", @"\\");

        // lambda

        verifier.Execute("""
ConsoleApp.Run(args, (string msg = @"\") => Console.Write(msg));
""", "", @"\");

        verifier.Execute("""
ConsoleApp.Run(args, (string msg = "\\") => Console.Write(msg));
""", "", "\\");

        verifier.Execute("""
ConsoleApp.Run(args, (string msg = @"\\") => Console.Write(msg));
""", "", @"\\");
    }

    [Fact]
    public void ShortNameAlias()
    {
        var code = """
var app = ConsoleApp.Create();
app.Add<FileCommand>();
app.Run(args);

public class FileCommand
{
    /// <summary>Outputs the provided file name.</summary>
    /// <param name="inputFile">-i, InputFile</param>
    [Command("")]
    public void Run(string inputFile) => Console.Write(inputFile);
}
""";

        verifier.Execute(code, "--input-file sample.txt", "sample.txt");
        verifier.Execute(code, "-i sample.txt", "sample.txt");
    }

    [Fact]
    public void ShortNameAndLongNameAlias()
    {
        var code = """
var app = ConsoleApp.Create();
app.Add<FileCommand>();
app.Run(args);

public class FileCommand
{
    /// <summary>Outputs the provided file name.</summary>
    /// <param name="inputFile">-i|--input, InputFile</param>
    [Command("")]
    public void Run(string inputFile) => Console.Write(inputFile);
}
""";

        verifier.Execute(code, "--input-file sample.txt", "sample.txt");
        verifier.Execute(code, "--input sample.txt", "sample.txt");
        verifier.Execute(code, "-i sample.txt", "sample.txt");
    }
}
