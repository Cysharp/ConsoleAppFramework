using ConsoleAppFramework;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks.Dataflow;
using DryIoc;

// args = "some-command hello --global-flag flag-value -- more args here".Split(" ");

var app = ConsoleApp.Create()
    // setup DryIoc as the DI container
    .ConfigureContainer(new DryIocServiceProviderFactory(), container =>
    {
        container.Register<MyService>();
    });

app.UseFilter<MyFilter>();
app.Add("", ([FromServices] MyService service) => service.Test());

app.Run(args);



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

[RegisterCommands("cmd")]
public class MyCommand
{
    [Command("test")]
    public int Test()
    {
        return 1;
    }
}
