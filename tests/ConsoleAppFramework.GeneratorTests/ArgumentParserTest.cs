using System.Diagnostics.CodeAnalysis;

namespace ConsoleAppFramework.GeneratorTests;

public class ArgumentParserTest(ITestOutputHelper output)
{
    readonly VerifyHelper verifier = new(output, "CAF");

    [Fact]
    public void Lamda()
    {
        verifier.Execute(HEAD + Body("""
            ConsoleApp.Run(args, ([Vector3Parser] Vector3 v) => Console.Write(v));
            """) + TAIL, args: "--v 1,2,3", expected: "<1, 2, 3>");
        verifier.Execute(HEAD + Body("""
            var app = ConsoleApp.Create();
            app.Add("", ([Vector3Parser] Vector3 v) => Console.Write(v));
            app.Run(args);
            """) + TAIL, args: "--v 1,2,3", expected: "<1, 2, 3>");
    }

    [Fact]
    public void Method()
    {
        verifier.Execute(HEAD + Body("""
            ConsoleApp.Run(args, MyCommands.Static);
            """) + TAIL, args: "--v 1,2,3", expected: "<1, 2, 3>");
        verifier.Execute(HEAD + Body("""
            var app = ConsoleApp.Create();
            app.Add("", MyCommands.Static);
            app.Run(args);
            """) + TAIL, args: "--v 1,2,3", expected: "<1, 2, 3>");
    }

    [Fact]
    public void Class()
    {
        verifier.Execute(HEAD + Body("""
            var app = ConsoleApp.Create();
            app.Add<MyCommands>();
            app.Run(args);
            """) + TAIL, args: "--v 1,2,3", expected: "<1, 2, 3>");
    }

    static string Body([StringSyntax("C#-test")] string code) => code;
    /// <summary>
    /// <see href="https://github.com/Cysharp/ConsoleAppFramework/blob/master/ReadMe.md#custom-value-converter"/>
    /// </summary>
    [StringSyntax("C#-test")]
    const string
        HEAD = """
        using System.Numerics;

        """,
        TAIL = """

        public class MyCommands
        {
            [Command("")]
            public void Root([Vector3Parser] Vector3 v) => Console.Write(v);
            public static void Static([Vector3Parser] Vector3 v) => Console.Write(v);
        }

        [AttributeUsage(AttributeTargets.Parameter)]
        public class Vector3ParserAttribute : Attribute, IArgumentParser<Vector3>
        {
            public static bool TryParse(ReadOnlySpan<char> s, out Vector3 result)
            {
                Span<Range> ranges = stackalloc Range[3];
                var splitCount = s.Split(ranges, ',');
                if (splitCount != 3)
                {
                    result = default;
                    return false;
                }

                float x;
                float y;
                float z;
                if (float.TryParse(s[ranges[0]], out x) && float.TryParse(s[ranges[1]], out y) && float.TryParse(s[ranges[2]], out z))
                {
                    result = new Vector3(x, y, z);
                    return true;
                }

                result = default;
                return false;
            }
        }
        """;
}
