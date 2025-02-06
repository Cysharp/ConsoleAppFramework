//using ConsoleAppFramework;

//namespace Cocona.Benchmark.External.Commands;

//public class ConsoleAppFrameworkCommand : ConsoleAppBase
//{
//    public void Execute(
//        [global::ConsoleAppFramework.Option("s")]
//        string? str,
//        [global::ConsoleAppFramework.Option("i")]
//        int intOption,
//        [global::ConsoleAppFramework.Option("b")]
//        bool boolOption)
//    {
//    }
//}

using ConsoleAppFramework;

public class ConsoleAppFrameworkCommand
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="str">-s</param>
    /// <param name="intOption">-i</param>
    /// <param name="boolOption">-b</param>
    public static void Execute(string? str, int intOption, bool boolOption)
    {

    }
}

public class ConsoleAppFrameworkCommandWithCancellationToken
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="str">-s</param>
    /// <param name="intOption">-i</param>
    /// <param name="boolOption">-b</param>
    public static void Execute(string? str, int intOption, bool boolOption, CancellationToken cancellationToken)
    {

    }
}

//internal class NopConsoleAppFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
//{
//    public override Task InvokeAsync(CancellationToken cancellationToken)
//    {
//        return Next.InvokeAsync(cancellationToken);
//    }
//}
