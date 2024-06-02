using ConsoleAppFramework;
using GeneratorSandbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;
using ZLogger;

// args = ["--msg", "foobarbaz"];


// Microsoft.Extensions.DependencyInjection

// Package Import: Microsoft.Extensions.Hosting
var builder = Host.CreateApplicationBuilder(); // don't pass args.

using var host = builder.Build(); // using
ConsoleApp.ServiceProvider = host.Services; // use host ServiceProvider

ConsoleApp.Run(args, ([FromServices] ILogger<Program> logger) => logger.LogInformation("Hello World!"));





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