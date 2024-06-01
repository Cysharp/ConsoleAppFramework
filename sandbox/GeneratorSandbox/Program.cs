using ConsoleAppFramework;
using System.ComponentModel.DataAnnotations;

var serviceCollection = new MiniDI();
serviceCollection.Register(typeof(string), "hoge!");
serviceCollection.Register(typeof(int), 9999);
ConsoleApp.ServiceProvider = serviceCollection;

var builder = ConsoleApp.Create();

builder.UseFilter<DIFilter>();

builder.Add("", (ConsoleAppContext ctx) => Console.Write("do"));

builder.Run(args);




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