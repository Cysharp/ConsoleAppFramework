using ConsoleAppFramework;
using Foo.Bar;
using System.ComponentModel.DataAnnotations;

var app = ConsoleApp.Create();
app.Add<Test>();
app.Add("", (int x, int y) => // different
{
    Console.WriteLine("body"); // body
});
app.Add("foo", () => { }); // newline
app.UseFilter<MyFilter>();
app.Run(args);

Console.WriteLine(""); // unrelated line

public class Test
{
    public void Show(string aaa, [Range(0, 1)] double value) => ConsoleApp.Log($"{value}");
}

namespace Foo.Bar
{
    public class MyFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
    {
        public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public class MyFilter2(ConsoleAppFilter next) : ConsoleAppFilter(next)
    {
        public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}