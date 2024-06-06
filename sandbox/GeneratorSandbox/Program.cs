using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using ConsoleAppFramework;



args = ["show", "--aaa", "a", "value", "10.2"];

var app = ConsoleApp.Create();
app.Add<Test>();
app.Run(args);

public class Test
{
    public void Show(string aaa, [Range(0, 1)] double value) => ConsoleApp.Log($"{value}");
}

