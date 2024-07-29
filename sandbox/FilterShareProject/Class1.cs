using ConsoleAppFramework;

namespace FilterShareProject;

public class TakoFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine("TAKO");
        return Next.InvokeAsync(context, cancellationToken);
    }
}

public class OtherProjectCommand
{
    public void Execute(int x)
    {
        Console.WriteLine("Hello?");
    }
}