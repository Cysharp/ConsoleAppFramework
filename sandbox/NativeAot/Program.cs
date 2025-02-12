using ConsoleAppFramework;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;

var app = ConsoleApp.Create();

ConsoleApp.Run(args, (int x, Kabayaki y) => Console.WriteLine(x + y.MyProperty));

app.Run(args);
public class Kabayaki
{
    public int MyProperty { get; set; }
}

