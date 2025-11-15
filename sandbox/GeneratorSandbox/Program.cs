using ConsoleAppFramework;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks.Dataflow;
using DryIoc;

// args = "some-command hello --global-flag flag-value -- more args here".Split(" ");


// Create a CancellationTokenSource that will be cancelled when 'Q' is pressed.
var cts = new CancellationTokenSource();
_ = Task.Run(() =>
{
    while (Console.ReadKey().Key != ConsoleKey.Q) ;
    Console.WriteLine();
    cts.Cancel();
});

var app = ConsoleApp.Create();

app.Add("", async (CancellationToken cancellationToken) =>
{
    // CancellationToken will be triggered when 'Q' is pressed or Ctrl+C(SIGINT/SIGTERM/SIGKILL) is sent.
    try
    {
        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine($"Running main task iteration {i + 1}/10. Press 'Q' to quit.");
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("Main task was cancelled.");
    }
});

await app.RunAsync(args, cts.Token); // pass external CancellationToken

//app.UseFilter<MyFilter>();
// app.Run(["cmd", "test"]);

public class MyService
{
    public void Test() => Console.WriteLine("Test");
}

internal class MyFilter(ConsoleAppFilter next, MyService myService) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        myService.Test();
        await Next.InvokeAsync(context, cancellationToken);
    }
}

//[RegisterCommands("cmd")]
//public class MyCommand
//{
//    [Command("test")]
//    public int Test()
//    {
//        return 1;
//    }
//}
