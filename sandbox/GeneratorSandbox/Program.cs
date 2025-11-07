using ConsoleAppFramework;

var builder = ConsoleApp.Create();
builder.Add<Commands>();
await builder.RunAsync(args);

public class Commands
{
    [Hidden]
    public void Command1() { Console.Write("command1"); }

    public void Command2() { Console.Write("command2"); }

    [Hidden]
    public void Command3(int x, [Hidden] int y) { Console.Write($"command3: x={x} y={y}"); }
}
