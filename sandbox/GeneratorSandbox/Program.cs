using ConsoleAppFramework;
using FilterShareProject;


args = ["Output"];

var app = ConsoleApp.Create();

app.Add<MyCommands>();

app.Run(args);



public class MyProjectCommand
{
    public void Execute(int x)
    {
        Console.WriteLine("Hello?");
    }
}


public class MyCommands
{
    [Command("Error1")]
    public void Error1(string msg = @"\")
    {
        Console.WriteLine(msg);
    }
    [Command("Error2")]
    public void Error2(string msg = "\\")
    {
        Console.WriteLine(msg);
    }
    [Command("Output")]
    public void Output(string msg = @"\\")
    {
        Console.WriteLine(msg); // 「\」
    }
}