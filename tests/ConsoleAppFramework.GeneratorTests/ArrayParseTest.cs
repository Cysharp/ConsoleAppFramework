namespace ConsoleAppFramework.GeneratorTests;

[ClassDataSource<VerifyHelper>]
public class ArrayParseTest(VerifyHelper verifier)
{
    [Test]
    public async Task Params()
    {
        var code = """
ConsoleApp.Run(args, (params int[] foo) =>
{
    Console.Write("[" + string.Join(", ", foo) + "]");
});
""";
        await verifier.Execute(code, args: "--foo", expected: "[]");
        await verifier.Execute(code, args: "--foo 10", expected: "[10]");
        await verifier.Execute(code, args: "--foo 10 20 30", expected: "[10, 20, 30]");
    }

    [Test]
    public async Task ArgumentParams()
    {
        var code = """
ConsoleApp.Run(args, ([Argument]string title, [Argument]params int[] foo) =>
{
    Console.Write(title + "[" + string.Join(", ", foo) + "]");
});
""";
        await verifier.Execute(code, args: "aiueo", expected: "aiueo[]");
        await verifier.Execute(code, args: "aiueo 10", expected: "aiueo[10]");
        await verifier.Execute(code, args: "aiueo 10 20 30", expected: "aiueo[10, 20, 30]");
    }

    [Test]
    public async Task ParseArray()
    {
        var code = """
ConsoleApp.Run(args, (int[] ix, string[] sx) =>
{
    Console.Write("[" + string.Join(", ", ix) + "]");
    Console.Write("[" + string.Join(", ", sx) + "]");
});
""";
        await verifier.Execute(code, args: "--ix 1,2,3,4,5 --sx a,b,c,d,e", expected: "[1, 2, 3, 4, 5][a, b, c, d, e]");

        var largeIntArray = string.Join(",", Enumerable.Range(0, 1000));
        var expectedIntArray = string.Join(", ", Enumerable.Range(0, 1000));
        await verifier.Execute(code, args: $"--ix {largeIntArray} --sx a,b,c,d,e", expected: $"[{expectedIntArray}][a, b, c, d, e]");
    }

    [Test]
    public async Task JsonArray()
    {
        var code = """
ConsoleApp.Run(args, (int[] ix, string[] sx) =>
{
    Console.Write("[" + string.Join(", ", ix) + "]");
    Console.Write("[" + string.Join(", ", sx) + "]");
});
""";
        await verifier.Execute(code, args: "--ix [] --sx []", expected: "[][]");
        await verifier.Execute(code, args: "--ix [1,2,3,4,5] --sx [\"a\",\"b\",\"c\",\"d\",\"e\"]", expected: "[1, 2, 3, 4, 5][a, b, c, d, e]");
    }

    [Test]
    public async Task RepeatedOption()
    {
        var code = """
ConsoleApp.Run(args, (int[] ix, string[] sx) =>
{
    Console.Write("[" + string.Join(", ", ix) + "]");
    Console.Write("[" + string.Join(", ", sx) + "]");
});
""";
        // single(unchanged)
        await verifier.Execute(code, args: "--ix 1 --sx a", expected: "[1][a]");
        // repeated
        await verifier.Execute(code, args: "--ix 1 --ix 2 --ix 3 --sx a --sx b", expected: "[1, 2, 3][a, b]");
        // interleaved
        await verifier.Execute(code, args: "--ix 1 --sx a --ix 2 --sx b --ix 3", expected: "[1, 2, 3][a, b]");
        // each occurrence is parsed as before(comma separated)
        await verifier.Execute(code, args: "--ix 1,2 --ix 3 --sx a --sx b,c", expected: "[1, 2, 3][a, b, c]");
        // each occurrence is parsed as before(json)
        await verifier.Execute(code, args: "--ix [1,2] --ix 3 --ix [4,5] --sx [\"a\"] --sx b", expected: "[1, 2, 3, 4, 5][a, b]");
        // empty array
        await verifier.Execute(code, args: "--ix [] --ix 1 --sx a --sx []", expected: "[1][a]");
        await verifier.Execute(code, args: "--ix [] --ix [] --sx [] --sx []", expected: "[][]");
        // ignore-case(slow path) is mixed
        await verifier.Execute(code, args: "--ix 1 --IX 2 --Ix 3 --SX a --sx b", expected: "[1, 2, 3][a, b]");
    }

    [Test]
    public async Task RepeatedOptionAlias()
    {
        var code = """
ConsoleApp.Run(args, Commands.Run);

static class Commands
{
    /// <param name="prop">-p, properties.</param>
    public static void Run(string[] prop)
    {
        Console.Write("[" + string.Join(", ", prop) + "]");
    }
}
""";
        await verifier.Execute(code, args: "-p a --prop b -p c", expected: "[a, b, c]");
        await verifier.Execute(code, args: "-P a --PROP b -p c,d", expected: "[a, b, c, d]");
    }

    [Test]
    public async Task RepeatedOptionDefaultValue()
    {
        var code = """
ConsoleApp.Run(args, (string[]? sx = null, int x = 0) =>
{
    Console.Write(sx == null ? "null" : "[" + string.Join(", ", sx) + "]");
});
""";
        // keep default value when not specified
        await verifier.Execute(code, args: "", expected: "null");
        await verifier.Execute(code, args: "--x 10", expected: "null");
        await verifier.Execute(code, args: "--sx a", expected: "[a]");
        await verifier.Execute(code, args: "--sx a --x 10 --sx b", expected: "[a, b]");
    }

    [Test]
    public async Task RepeatedOptionRequired()
    {
        var code = """
ConsoleApp.LogError = x => Console.Write(x);

ConsoleApp.Run(args, (int[] ix, int x = 0) =>
{
    Console.Write("[" + string.Join(", ", ix) + "]");
});
""";
        await verifier.Execute(code, args: "--ix 1 --x 10 --ix 2", expected: "[1, 2]");

        var (stdout, exitCode) = verifier.Error(code, "--x 10");
        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(stdout).Contains("Required argument 'ix' was not specified.");

        // parse failed in second occurrence
        (stdout, exitCode) = verifier.Error(code, "--ix 1 --ix foo");
        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(stdout).Contains("Argument 'ix' failed to parse, provided value: foo");
    }

    [Test]
    public async Task RepeatedOptionJsonElement()
    {
        // element type is not ISpanParsable, each occurrence is parsed as JSON
        var code = """
ConsoleApp.Run(args, (MyItem[] items, MyEnum[] enums) =>
{
    Console.Write("[" + string.Join<MyItem>(", ", items) + "]");
    Console.Write("[" + string.Join(", ", enums) + "]");
});

public class MyItem
{
    public int X { get; set; }

    public override string ToString() => X.ToString();
}

public enum MyEnum
{
    A, B, C
}
""";
        await verifier.Execute(code, args: "--items [{\"X\":1}] --enums [0]", expected: "[1][A]");
        await verifier.Execute(code, args: "--items [{\"X\":1}] --enums [0,1] --items [{\"X\":2},{\"X\":3}] --enums [2]", expected: "[1, 2, 3][A, B, C]");
    }

    [Test]
    public async Task RepeatedOptionCustomParser()
    {
        // custom parser can disable comma separated parsing, each occurrence is appended as well
        var code = """
ConsoleApp.Run(args, ([NoSplit] string[] p) =>
{
    Console.Write(string.Join("|", p));
});

[AttributeUsage(AttributeTargets.Parameter)]
public class NoSplitAttribute : Attribute, IArgumentParser<string[]>
{
    public static bool TryParse(ReadOnlySpan<char> s, out string[] result)
    {
        result = [s.ToString()];
        return true;
    }
}
""";
        await verifier.Execute(code, args: "--p List=a,b", expected: "List=a,b");
        await verifier.Execute(code, args: "--p List=a,b --p X=1 --p [1,2]", expected: "List=a,b|X=1|[1,2]");
    }

    [Test]
    public async Task RepeatedOptionBuilder()
    {
        var code = """
var app = ConsoleApp.Create();
app.Add("", (int[] ix) => Console.Write("[" + string.Join(", ", ix) + "]"));
app.Add<MyCommands>();
app.Run(args);

public class MyCommands
{
    public void Foo(string[] sx, int[]? ix = null)
    {
        Console.Write("[" + string.Join(", ", sx) + "]");
        Console.Write(ix == null ? "null" : "[" + string.Join(", ", ix) + "]");
    }
}
""";
        await verifier.Execute(code, args: "--ix 1 --ix 2,3", expected: "[1, 2, 3]");
        await verifier.Execute(code, args: "foo --sx a --sx b", expected: "[a, b]null");
        await verifier.Execute(code, args: "foo --ix 1 --sx a --ix 2 --sx b", expected: "[a, b][1, 2]");
    }

    [Test]
    public async Task ArgumentArrayIsNotRepeated()
    {
        // [Argument] is positional so each argument is bound to each parameter
        var code = """
ConsoleApp.Run(args, ([Argument] int[] ix, [Argument] int[] iy, int[] iz) =>
{
    Console.Write("[" + string.Join(", ", ix) + "]");
    Console.Write("[" + string.Join(", ", iy) + "]");
    Console.Write("[" + string.Join(", ", iz) + "]");
});
""";
        await verifier.Execute(code, args: "1,2 3,4 --iz 5 --iz 6", expected: "[1, 2][3, 4][5, 6]");
        await verifier.Execute(code, args: "--iz 5 1,2 --iz 6 3,4", expected: "[1, 2][3, 4][5, 6]");
    }
}
