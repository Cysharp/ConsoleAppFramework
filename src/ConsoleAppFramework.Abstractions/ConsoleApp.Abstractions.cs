namespace ConsoleAppFramework;

public interface IArgumentParser<T>
{
    static abstract bool TryParse(ReadOnlySpan<char> s, out T result);
}

public record class ConsoleAppContext(string CommandName, string[] Arguments, object? State);

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