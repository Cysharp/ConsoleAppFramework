using System.ComponentModel;

namespace ConsoleAppFramework;

public interface IArgumentParser<T>
{
    static abstract bool TryParse(ReadOnlySpan<char> s, out T result);
}

/// <summary>
/// Represents the execution context for a console application command, containing command metadata and parsed arguments.
/// </summary>
public record ConsoleAppContext
{
    /// <summary>
    /// Gets the name of the command being executed.
    /// </summary>
    public string CommandName { get; init; }

    /// <summary>
    /// Gets the raw arguments passed to the application, including the command name itself.
    /// </summary>
    public ReadOnlyMemory<string> Arguments { get; init; }

    /// <summary>
    /// Gets the custom state object that can be used to share data across commands.
    /// </summary>
    public object? State { get; init; }

    /// <summary>
    /// Gets the parsed global options that apply across all commands.
    /// </summary>
    public object? GlobalOptions { get; init; }

    /// <summary>
    /// Gets the depth of the command in a nested command hierarchy. Used internally by the framework.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public int CommandDepth { get; }

    /// <summary>
    /// Gets the index of the escape separator ('--') in the arguments, or -1 if not present. Used internally by the framework.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public int EscapeIndex { get; }

    /// <summary>
    /// Gets the internal command arguments with global options removed. Used internally by the framework.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ReadOnlyMemory<string> InternalCommandArgs { get; }

    /// <summary>
    /// Gets the arguments intended for the current command, excluding the command name and any escaped arguments after '--'.
    /// </summary>
    public ReadOnlySpan<string> CommandArguments
    {
        get => (EscapeIndex == -1)
            ? Arguments.Span.Slice(CommandDepth)
            : Arguments.Span.Slice(CommandDepth, EscapeIndex - CommandDepth);
    }

    /// <summary>
    /// Gets the arguments that appear after the escape separator ('--'), which are not parsed by the command.
    /// Returns an empty span if no escape separator is present.
    /// </summary>
    public ReadOnlySpan<string> EscapedArguments
    {
        get => (EscapeIndex == -1)
            ? Array.Empty<string>()
            : Arguments.Span.Slice(EscapeIndex + 1);
    }

    public ConsoleAppContext(string commandName, ReadOnlyMemory<string> arguments, ReadOnlyMemory<string> internalCommandArgs, object? state, object? globalOptions, int commandDepth, int escapeIndex)
    {
        this.CommandName = commandName;
        this.Arguments = arguments;
        this.InternalCommandArgs = internalCommandArgs;
        this.State = state;
        this.GlobalOptions = globalOptions;
        this.CommandDepth = commandDepth;
        this.EscapeIndex = escapeIndex;
    }

    /// <summary>
    /// Returns a string representation of all arguments joined by spaces.
    /// </summary>
    /// <returns>A space-separated string of all arguments.</returns>
    public override string ToString()
    {
        return string.Join(" ", Arguments.ToArray());
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
