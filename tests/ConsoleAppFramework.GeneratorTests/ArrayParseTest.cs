using Xunit.Abstractions;

namespace ConsoleAppFramework.GeneratorTests
{
    public class ArrayParseTest(ITestOutputHelper output)
    {
        VerifyHelper verifier = new VerifyHelper(output, "CAF");

        [Fact]
        public void Params()
        {
            var code = """
ConsoleApp.Run(args, (params int[] foo) =>
{
    Console.Write("[" + string.Join(", ", foo) + "]");
});
""";
            verifier.Execute(code, args: "--foo", expected: "[]");
            verifier.Execute(code, args: "--foo 10", expected: "[10]");
            verifier.Execute(code, args: "--foo 10 20 30", expected: "[10, 20, 30]");
        }

        [Fact]
        public void ArgumentParams()
        {
            var code = """
ConsoleApp.Run(args, ([Argument]string title, [Argument]params int[] foo) =>
{
    Console.Write(title + "[" + string.Join(", ", foo) + "]");
});
""";
            verifier.Execute(code, args: "aiueo", expected: "aiueo[]");
            verifier.Execute(code, args: "aiueo 10", expected: "aiueo[10]");
            verifier.Execute(code, args: "aiueo 10 20 30", expected: "aiueo[10, 20, 30]");
        }

        [Fact]
        public void ParseArray()
        {
            var code = """
ConsoleApp.Run(args, (int[] ix, string[] sx) =>
{
    Console.Write("[" + string.Join(", ", ix) + "]");
    Console.Write("[" + string.Join(", ", sx) + "]");
});
""";
            verifier.Execute(code, args: "--ix 1,2,3,4,5 --sx a,b,c,d,e", expected: "[1, 2, 3, 4, 5][a, b, c, d, e]");

            var largeIntArray = string.Join(",", Enumerable.Range(0, 1000));
            var expectedIntArray = string.Join(", ", Enumerable.Range(0, 1000));
            verifier.Execute(code, args: $"--ix {largeIntArray} --sx a,b,c,d,e", expected: $"[{expectedIntArray}][a, b, c, d, e]");
        }

        [Fact]
        public void JsonArray()
        {
            var code = """
ConsoleApp.Run(args, (int[] ix, string[] sx) =>
{
    Console.Write("[" + string.Join(", ", ix) + "]");
    Console.Write("[" + string.Join(", ", sx) + "]");
});
""";
            verifier.Execute(code, args: "--ix [] --sx []", expected: "[][]");
            verifier.Execute(code, args: "--ix [1,2,3,4,5] --sx [\"a\",\"b\",\"c\",\"d\",\"e\"]", expected: "[1, 2, 3, 4, 5][a, b, c, d, e]");
        }
    }
}
