namespace ConsoleAppFramework.GeneratorTests;

public class Test : IDisposable
{
    VerifyHelper verifier = new VerifyHelper("CAF");

    public void Dispose() => Environment.ExitCode = 0;

    [Test]
    public async Task SyncRun()
    {
        await verifier.Execute("ConsoleApp.Run(args, (int x, int y) => { Console.Write((x + y)); });", "--x 10 --y 20", "30");
    }

    [Test]
    public async Task OptionTokenShouldNotFillArgumentSlot()
    {
        var code = """
ConsoleApp.Run(args, ([Argument] string path, bool dryRun) =>
{
    Console.Write((dryRun, path).ToString());
});
""";

        await Assert.That(verifier.Error(code, "--dry-run")).Contains("Required argument 'path' was not specified.");
        await verifier.Execute(code, "--dry-run sample.txt", "(True, sample.txt)");
    }

    [Test]
    public async Task OptionTokenAllowsMultipleArguments()
    {
        var code = """
ConsoleApp.Run(args, ([Argument] string source, [Argument] string destination, bool dryRun) =>
{
    Console.Write((dryRun, source, destination).ToString());
});
""";

        await verifier.Execute(code, "--dry-run input.json output.json", "(True, input.json, output.json)");
    }

    [Test]
    public async Task OptionTokenRespectsArgumentDefaultValue()
    {
        var code = """
ConsoleApp.Run(args, ([Argument] string path = "default-path", bool dryRun = false) =>
{
    Console.Write((dryRun, path).ToString());
});
""";

        await verifier.Execute(code, "--dry-run", "(True, default-path)");
    }

    [Test]
    public async Task OptionTokenHandlesParamsArguments()
    {
        var code = """
ConsoleApp.Run(args, ([Argument] string path, bool dryRun, params string[] extras) =>
{
    Console.Write($"{dryRun}:{path}:{string.Join("|", extras)}");
});
""";

        await verifier.Execute(code, "--dry-run path.txt --extras src.txt dst.txt", "True:path.txt:src.txt|dst.txt");
        await verifier.Execute(code, "--dry-run path.txt", "True:path.txt:");
    }

    [Test]
    public async Task ArgumentAllowsLeadingDashValue()
    {
        var code = """
ConsoleApp.Run(args, ([Argument] int count, bool dryRun) =>
{
    Console.Write((count, dryRun).ToString());
});
""";

        await verifier.Execute(code, "-5 --dry-run", "(-5, True)");
        await verifier.Execute(code, "-5", "(-5, False)");
    }

    [Test]
    public async Task SyncRunShouldFailed()
    {
        await Assert.That(verifier.Error("ConsoleApp.Run(args, (int x) => { Console.Write((x)); });", "--x")).Contains("Argument 'x' failed to parse");
    }

    [Test]
    public async Task MissingArgument()
    {
        await Assert.That(verifier.Error("ConsoleApp.Run(args, (int x, int y) => { Console.Write((x + y)); });", "--x 10 y 20")).Contains("Argument 'y' is not recognized.");

        await Assert.That(Environment.ExitCode).IsEqualTo(1);
        Environment.ExitCode = 0;
    }

    [Test]
    public async Task ValidateOne()
    {
        var expected = """
The field x must be between 1 and 10.


""";

        await verifier.Execute("""
ConsoleApp.Run(args, ([Range(1, 10)]int x, [Range(100, 200)]int y) => { Console.Write((x + y)); });
""", "--x 100 --y 140", expected);

        await Assert.That(Environment.ExitCode).IsEqualTo(1);
        Environment.ExitCode = 0;
    }

    [Test]
    public async Task ValidateTwo()
    {
        var expected = """
The field x must be between 1 and 10.
The field y must be between 100 and 200.


""";

        await verifier.Execute("""
ConsoleApp.Run(args, ([Range(1, 10)]int x, [Range(100, 200)]int y) => { Console.Write((x + y)); });
""", "--x 100 --y 240", expected);

        await Assert.That(Environment.ExitCode).IsEqualTo(1);
        Environment.ExitCode = 0;
    }
    [Test]
    public async Task Parameters()
    {
        await verifier.Execute("""
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

    [Test]
    public async Task ValidateClass()
    {
        var expected = """
The field value must be between 0 and 1.


""";

        await verifier.Execute("""
var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test
{
    public async Task Show(string aaa, [Range(0, 1)] double value) => ConsoleApp.Log($"{value}");
}

""", "show --aaa foo --value 100", expected);

    }

    [Test]
    public async Task StringEscape()
    {
        var code = """
var app = ConsoleApp.Create();
app.Add<MyCommands>();
app.Run(args);

public class MyCommands
{
    [Command("Error1")]
    public async Task Error1(string msg = @"\")
    {
        Console.Write(msg);
    }
    [Command("Error2")]
    public async Task Error2(string msg = "\\")
    {
        Console.Write(msg);
    }
    [Command("Output")]
    public async Task Output(string msg = @"\\")
    {
        Console.Write(msg); 
    }
}
""";

        await verifier.Execute(code, "Error1", @"\");
        await verifier.Execute(code, "Error2", "\\");
        await verifier.Execute(code, "Output", @"\\");

        // lambda

        await verifier.Execute("""
ConsoleApp.Run(args, (string msg = @"\") => Console.Write(msg));
""", "", @"\");

        await verifier.Execute("""
ConsoleApp.Run(args, (string msg = "\\") => Console.Write(msg));
""", "", "\\");

        await verifier.Execute("""
ConsoleApp.Run(args, (string msg = @"\\") => Console.Write(msg));
""", "", @"\\");
    }

    [Test]
    public async Task ShortNameAlias()
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
    public async Task Run(string inputFile) => Console.Write(inputFile);
}
""";

        await verifier.Execute(code, "--input-file sample.txt", "sample.txt");
        await verifier.Execute(code, "-i sample.txt", "sample.txt");
    }

    [Test]
    public async Task ShortNameAndLongNameAlias()
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
    public async Task Run(string inputFile) => Console.Write(inputFile);
}
""";

        await verifier.Execute(code, "--input-file sample.txt", "sample.txt");
        await verifier.Execute(code, "--input sample.txt", "sample.txt");
        await verifier.Execute(code, "-i sample.txt", "sample.txt");
    }

    [Test]
    public async Task ArgumentLastParams()
    {
        var code = """
ConsoleApp.Run(args, (string opt1, [Argument]params string[] args) =>
{
    Console.Write($"{opt1}, {string.Join("|", args)}");
});
""";

        await verifier.Execute(code, "--opt1 abc a b c d", "abc, a|b|c|d");
    }

    [Test]
    public async Task RunAndRunAsyncOverloads()
    {
        await verifier.Execute("""
ConsoleApp.Run(args, () => Console.Write("sync"));
""", "", "sync");

        await verifier.Execute("""
ConsoleApp.Run(args, async () => Console.Write("async"));
""", "", "async");

        await verifier.Execute("""
await ConsoleApp.RunAsync(args, () => Console.Write("sync"));
""", "", "sync");

        await verifier.Execute("""
await ConsoleApp.RunAsync(args, async () => Console.Write("async"));
""", "", "async");
    }

    [Test]
    public async Task RunAndRunAsyncOverloadsWithCancellationToken()
    {
        await verifier.Execute("""
ConsoleApp.Run(args, (CancellationToken cancellationToken) => Console.Write("sync"));
""", "", "sync");

        await verifier.Execute("""
ConsoleApp.Run(args, async (CancellationToken cancellationToken) => Console.Write("async"));
""", "", "async");

        await verifier.Execute("""
await ConsoleApp.RunAsync(args, (CancellationToken cancellationToken) => Console.Write("sync"));
""", "", "sync");

        await verifier.Execute("""
await ConsoleApp.RunAsync(args, async (CancellationToken cancellationToken) => Console.Write("async"));
""", "", "async");
    }
}
