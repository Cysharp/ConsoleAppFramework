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

args = ["--hello", "10", "--world", "20"]; // test.


// var s = "foo";
// s.AsSpan().Split(',',).


// BigInteger.TryParse(
// Version.TryParse(

// Uri.TryCreate(UriCreationOptions


// IParsable<Complex>.TryParse(

// --x

unsafe
{

    ConsoleApp.Run(args, &Command.Execute);

}

// description
// 

var sc = new ServiceCollection();
sc.AddSingleton<MyClass>();
var provider = sc.BuildServiceProvider();
ConsoleApp.ServiceProvider = provider;


static void RunRun(int x, int y)
{
    Console.WriteLine("Hello World!" + x + y);
}


public static class Command
{
    /// <summary>
    /// Fuga Fuga
    /// </summary>
    /// <param name="hello">-h, left x</param>
    /// <param name="world">-w|--woorong, world</param>
    public static void Execute(int hello, int world = 12345, CancellationToken cancellationToken = default)
    {
        Console.WriteLine("go");
        Thread.Sleep(TimeSpan.FromSeconds(10));
        Console.WriteLine("end");
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
    //partial class ConsoleApp
    //{


    //    public ConsoleAppBuilder CreateBuilder()
    //    {
    //        return new ConsoleAppBuilder();
    //    }
    //}


    //public class ConsoleAppBuilder
    //{
    //    public void Add()
    //    {
    //    }



    //    public void Run(string[] args)
    //    {
    //        if (args.Length == 0 || args[0].StartsWith('-'))
    //        {
    //            // invoke root command
    //        }
    //    }

    //    public void RunAsync(string[] args)
    //    {
    //    }
    //}
}

