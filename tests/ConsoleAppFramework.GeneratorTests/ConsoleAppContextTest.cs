namespace ConsoleAppFramework.GeneratorTests;

public class ConsoleAppContextTest(ITestOutputHelper output)
{
    VerifyHelper verifier = new VerifyHelper(output, "CAF");

    [Fact]
    public void ForLambda()
    {
        verifier.Execute("""
ConsoleApp.Run(args, (ConsoleAppContext ctx) => { Console.Write(ctx.Arguments.Length); });
""", args: "", expected: "0");
    }

    [Fact]
    public void ForMethod()
    {
        verifier.Execute("""
var builder = ConsoleApp.Create();

builder.UseFilter<StateFilter>();

builder.Add("", Hello);

builder.Run(args);

void Hello(ConsoleAppContext ctx)
{
    Console.Write(ctx.State);
}

internal class StateFilter(ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context,CancellationToken cancellationToken)
    {
        Console.Write(1);
        return Next.InvokeAsync(context with { State = 2 }, cancellationToken);
    }
}
""", args: "", expected: "12");
    }

    [Theory]
    [InlineData("--x 1 --y 2", "", "--x 1 --y 2", "")] // no command, no espace
    [InlineData("foo --x 1 --y 2", "foo", "--x 1 --y 2", "")] // command, no espace
    [InlineData("foo bar --x 1 --y 2", "foo bar", "--x 1 --y 2", "")] // nested command, no espace
    [InlineData("--x 1 --y 2 -- abc", "", "--x 1 --y 2", "abc")] // no command, espace
    [InlineData("--x 1 --y 2 -- abc def", "", "--x 1 --y 2", "abc def")] // no command, espace2
    [InlineData("foo --x 1 --y 2 -- abc", "foo", "--x 1 --y 2", "abc")] // command, espace
    [InlineData("foo --x 1 --y 2 -- abc def", "foo", "--x 1 --y 2", "abc def")] // command, espace2
    [InlineData("foo bar --x 1 --y 2 -- abc", "foo bar", "--x 1 --y 2", "abc")] // nested command, espace
    [InlineData("foo bar --x 1 --y 2 -- abc def", "foo bar", "--x 1 --y 2", "abc def")] // nested command, espace2
    public void ArgumentsParseTest(string args, string commandName, string expectedCommandArguments, string expectedEscapedArguments)
    {
        var argsSpan = args.Split(' ').AsSpan();
        var commandDepth = (commandName == "") ? 0 : (argsSpan.Length - args.Replace(commandName, "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
        var escapeIndex = argsSpan.IndexOf("--");

        var ctx = new ConsoleAppContext2(commandName, argsSpan.ToArray(), null, commandDepth, escapeIndex);

        string.Join(" ", ctx.CommandArguments!).ShouldBe(expectedCommandArguments);
        string.Join(" ", ctx.EscapedArguments!).ShouldBe(expectedEscapedArguments);
    }

    public class ConsoleAppContext2
    {
        public string CommandName { get; }
        public string[] Arguments { get; }
        public object? State { get; }

        int commandDepth;
        int escapeIndex;

        public ReadOnlySpan<string> CommandArguments
        {
            get => (escapeIndex == -1)
                ? Arguments.AsSpan(commandDepth)
                : Arguments.AsSpan(commandDepth, escapeIndex - commandDepth);
        }

        public ReadOnlySpan<string> EscapedArguments
        {
            get => (escapeIndex == -1)
                ? Array.Empty<string>()
                : Arguments.AsSpan(escapeIndex + 1);
        }

        public ConsoleAppContext2(string commandName, string[] arguments, object? state, int commandDepth, int escapeIndex)
        {
            this.CommandName = commandName;
            this.Arguments = arguments;
            this.State = state;

            this.commandDepth = commandDepth;
            this.escapeIndex = escapeIndex;
        }

        public override string ToString()
        {
            return string.Join(" ", Arguments);
        }
    }
}
