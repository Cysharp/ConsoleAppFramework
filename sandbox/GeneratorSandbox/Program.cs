using ConsoleAppFramework;
using GeneratorSandbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Threading.Channels;
using ZLogger;


args = "--first-arg invalid.email --second-arg 10".Split(' ');

ConsoleApp.Timeout = Timeout.InfiniteTimeSpan;




ConsoleApp.Run(args, (
    [Argument] DateTime dateTime, // Argument
    [Argument] Guid guidvalue,    // 
    int intVar,                   // required
    bool boolFlag,                // flag
    MyEnum enumValue,             // enum
    int[] array,                  // array
    MyClass obj,                  // object
    string optional = "abcde",    // optional
    double? nullableValue = null, // nullable
    params string[] paramsArray   // params
    ) => { });








public enum MyEnum
{

}

public class MyClass
{

}

// inject logger
public class MyCommand(ILogger<MyCommand> logger, IOptions<PositionOptions> options)
{
    [Command("")]
    public void Echo(string msg)
    {
        logger.ZLogTrace($"Binded Option: {options.Value.Title} {options.Value.Name}");
        logger.ZLogInformation($"Message is {msg}");
    }
}



public class PositionOptions
{
    public string Title { get; set; } = "";
    public string Name { get; set; } = "";
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


internal class DIFilter(string foo, int bar, ConsoleAppFilter next)
    : ConsoleAppFilter(next)
{
    public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        var newContext = context with { State = 100 };

        Console.Write("invoke:");
        Console.Write(foo);
        Console.Write(bar);
        return Next.InvokeAsync(newContext, cancellationToken);
    }
}

public class MiniDI : IServiceProvider
{
    System.Collections.Generic.Dictionary<Type, object> dict = new();

    public void Register(Type type, object instance)
    {
        dict[type] = instance;
    }

    public object? GetService(Type serviceType)
    {
        return dict.TryGetValue(serviceType, out var instance) ? instance : null;
    }
}

namespace ConsoleAppFramework
{
    partial class ConsoleApp
    {
        static async Task RunWithFilterAsync2(string commandName, string[] args, ConsoleAppFilter invoker)
        {
            using var posixSignalHandler = PosixSignalHandler.Register(Timeout);
            try
            {


                await Task.Run(() => invoker.InvokeAsync(new ConsoleAppContext(commandName, args, null), posixSignalHandler.Token)).WaitAsync(posixSignalHandler.TimeoutToken);


                await Task.Factory.StartNew(static state => Task.CompletedTask, 1983, default, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default).Unwrap();



            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    Environment.ExitCode = 130;
                    return;
                }

                Environment.ExitCode = 1;
                if (ex is ValidationException)
                {
                    LogError(ex.Message);
                }
                else
                {
                    LogError(ex.ToString());
                }
            }
        }
    }
}