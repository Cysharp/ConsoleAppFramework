using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Takoyaki;

args = ["--x", "10"]; // test.


// var s = "foo";
// s.AsSpan().Split(',',).


// BigInteger.TryParse(
// Version.TryParse(

// Uri.TryCreate(UriCreationOptions


// IParsable<Complex>.TryParse(

// --x



// description
// 

var sc = new ServiceCollection();
sc.AddSingleton<MyClass>();
var provider = sc.BuildServiceProvider();
ConsoleApp.ServiceProvider = provider;


//var cts = new CancellationTokenSource();

//var iii = 0;
//while (true)
//{
//    Thread.Sleep(TimeSpan.FromSeconds(1));
//    Console.WriteLine(iii++ + ", " + cts.IsCancellationRequested);
//}


//delegate* managed<int, int, void> a = &Method;

// sp.GetService();

await ConsoleApp.RunAsync(args, static async (int x, [FromServices] MyClass mc, CancellationToken cancellationToken) =>
{
    Console.WriteLine((x, mc));
    await Task.Yield();
    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
    Console.WriteLine("end");
});


//ConsoleApp.Run(args, &Methods.Method);

static void Foo(RunRun rrrr)
{
}


internal delegate void RunRun(int x, int y = 100);


public static class Methods
{
    public static void Method(int x, int y = 12345)
    {

    }
}

public class MyClass
{

}



namespace Takoyaki
{
    public enum MyEnum
    {

    }

    public static class Hoge
    {
        public static void Nano(int x)
        {
        }
    }
}



//public class MyClass
//{
//    /// <param name="takoyaki">--tako, -t, foo bar baz.</param>
//    public void Foo(int takoyaki, int y)
//    {
//    }
//}


public interface IParser<T>
{
    static abstract bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out T result);
}





public readonly struct Vector3Parser : IParser<Vector3>
{
    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out Vector3 result)
    {
        Span<Range> ranges = stackalloc Range[3];
        var splitCount = s.AsSpan().Split(ranges, ',');
        if (splitCount != 3)
        {
            result = default;
            return false;
        }

        float x;
        float y;
        float z;
        if (float.TryParse(s.AsSpan(ranges[0]), out x) && float.TryParse(s.AsSpan(ranges[1]), out y) && float.TryParse(s.AsSpan(ranges[2]), out z))
        {
            result = new Vector3(x, y, z);
            return true;
        }

        result = default;
        return false;
    }
}

namespace ConsoleAppFramework
{


}

