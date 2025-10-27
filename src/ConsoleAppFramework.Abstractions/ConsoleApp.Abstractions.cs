using System.ComponentModel;

namespace ConsoleAppFramework;

public interface IArgumentParser<T>
{
    static abstract bool TryParse(ReadOnlySpan<char> s, out T result);
}

public record ConsoleAppContext
{
    public string CommandName { get; init; }
    public string[] Arguments { get; init; }
    public object? State { get; init; }
    public object? GlobalOptions { get; init; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public int CommandDepth { get; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public int EscapeIndex { get; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public string[] InternalCommandArgs { get; }

    public ReadOnlySpan<string> CommandArguments
    {
        get => (EscapeIndex == -1)
            ? Arguments.AsSpan(CommandDepth)
            : Arguments.AsSpan(CommandDepth, EscapeIndex - CommandDepth);
    }

    public ReadOnlySpan<string> EscapedArguments
    {
        get => (EscapeIndex == -1)
            ? Array.Empty<string>()
            : Arguments.AsSpan(EscapeIndex + 1);
    }

    public ConsoleAppContext(string commandName, string[] arguments, string[] commandArgs, object? state, object? globalOptions, int commandDepth, int escapeIndex)
    {
        this.CommandName = commandName;
        this.Arguments = arguments;
        this.InternalCommandArgs = commandArgs;
        this.State = state;
        this.GlobalOptions = globalOptions;
        this.CommandDepth = commandDepth;
        this.EscapeIndex = escapeIndex;
    }

    public override string ToString()
    {
        return string.Join(" ", Arguments);
    }
}

public abstract class ConsoleAppFilter(ConsoleAppFilter next)
{
    protected readonly ConsoleAppFilter Next = next;

    public abstract Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken);
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ConsoleAppFilterAttribute<T> : Attribute
    where T : ConsoleAppFilter
{
}

public sealed class ArgumentParseFailedException(string message) : Exception(message)
{
}
