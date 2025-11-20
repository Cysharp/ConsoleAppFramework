using Autofac;
using Autofac.Extensions.DependencyInjection;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;


var app = ConsoleApp.Create()
    .ConfigureContainer(new AutofacServiceProviderFactory(), builder =>
    {
        builder.RegisterType<MyService>();
    });

app.Add("", ([FromServices] MyService service) => { service.Hello(); });

app.Run(args);

public class MyService
{
    public void Hello() => Console.WriteLine("Hello from MyService");
}
