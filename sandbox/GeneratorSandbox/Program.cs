using ConsoleAppFramework;
using FilterShareProject;



var app = ConsoleApp.Create();

var v = new OtherProjectCommand();
// app.Add("", v.Execute);
app.Add<MyProjectCommand>();

app.Run(args);



public class MyProjectCommand
{
    public void Execute(int x)
    {
        Console.WriteLine("Hello?");
    }
}