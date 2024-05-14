using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;


args = ["100", "--y", "1,10,100,100,1000"]; // test.


// var s = "foo";
// s.AsSpan().Split(',',).


// BigInteger.TryParse(
// Version.TryParse(

// Uri.TryCreate(UriCreationOptions


// IParsable<Complex>.TryParse(

// --x

unsafe
{

    //ConsoleApp.Run(args, ([Vector3Parser] Vector3 x) =>
    //{Quaternion//
    //});
    ConsoleApp.Run(args, ([Argument] int x, int[] y) =>
    {
        Console.WriteLine((x, y));
    });


}

// description
// 

var sc = new ServiceCollection();
sc.AddSingleton<MyClass>();
var provider = sc.BuildServiceProvider();
ConsoleApp.ServiceProvider = provider;






static async Task<int> RunRun([Vector3Parser] Vector3 x, [FromServices] MyClass y)
{
    Console.WriteLine("Hello World!" + x + y);
    return 0;
}


static void Tests<T>()
    where T : ISpanParsable<int>
{


}


public static class Command
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="foo">-f|--cho|/tako|*nano|-ZOMBI</param>
    /// <param name="cancellationToken"></param>
    public static void Execute(int foo, CancellationToken cancellationToken)
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


//public interface IArgumentParser<T>
//{
//    static abstract bool TryParse(ReadOnlySpan<char> s, out T result);
//}


[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class ArgumentAttribute : Attribute
{
}



[AttributeUsage(AttributeTargets.Parameter)]
public class Vector3ParserAttribute : Attribute, IArgumentParser<Vector3>
{
    public static bool TryParse(ReadOnlySpan<char> s, out Vector3 result)
    {
        Span<Range> ranges = stackalloc Range[3];
        var splitCount = s.Split(ranges, ',');
        if (splitCount != 3)
        {
            result = default;
            return false;
        }

        float x;
        float y;
        float z;
        if (float.TryParse(s[ranges[0]], out x) && float.TryParse(s[ranges[1]], out y) && float.TryParse(s[ranges[2]], out z))
        {
            result = new Vector3(x, y, z);
            return true;
        }

        result = default;
        return false;
    }
}



[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class ArrayParserAttribute<T> : Attribute, IArgumentParser<T[]>
    where T : ISpanParsable<T>
{
    public static bool TryParse(ReadOnlySpan<char> s, out T[] result)
    {
        var count = s.Count(',') + 1;
        result = new T[count];

        var source = s;
        var destination = result.AsSpan();
        Span<Range> ranges = stackalloc Range[Math.Min(count, 128)];

        while (true)
        {
            var splitCount = source.Split(ranges, ',');
            var parseTo = splitCount;
            if (splitCount == 128 && source[ranges[^1]].Contains(',')) // check have more region
            {
                parseTo = splitCount - 1;
            }

            for (int i = 0; i < parseTo; i++)
            {
                if (!T.TryParse(source[ranges[i]], null, out destination[i]!))
                {
                    return false;
                }
            }
            destination = destination.Slice(parseTo);

            if (destination.Length != 0)
            {
                source = source[ranges[^1]];
                continue;
            }
            else
            {
                break;
            }
        }

        return true;
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

    partial class ConsoleApp

    {
        static bool TrySplitParse2<T>(ReadOnlySpan<char> s, out T[] result)
       where T : ISpanParsable<T>
        {
            if (s.StartsWith("["))
            {
                try
                {
                    result = System.Text.Json.JsonSerializer.Deserialize<T[]>(s)!;
                }
                catch
                {
                    result = default!;
                    return false;
                }
            }

            var count = s.Count(',') + 1;
            result = new T[count];

            var source = s;
            var destination = result.AsSpan();
            Span<Range> ranges = stackalloc Range[Math.Min(count, 128)];

            while (true)
            {
                var splitCount = source.Split(ranges, ',');
                var parseTo = splitCount;
                if (splitCount == 128 && source[ranges[^1]].Contains(','))
                {
                    parseTo = splitCount - 1;
                }

                for (int i = 0; i < parseTo; i++)
                {
                    if (!T.TryParse(source[ranges[i]], null, out destination[i]!))
                    {
                        return false;
                    }
                }
                destination = destination.Slice(parseTo);

                if (destination.Length != 0)
                {
                    source = source[ranges[^1]];
                    continue;
                }
                else
                {
                    break;
                }
            }

            return true;
        }

        public static void Run2(string[] args, Action<int, int> command)
        {
            var arg0 = default(int);
            var arg0Parsed = false;
            var arg1 = default(int);
            var arg1Parsed = false;

            try
            {
                for (int i = 0; i < args.Length; i++)
                {
                    // add this block.
                    if (i == 0)
                    {
                        if (!int.TryParse(args[i], out arg0)) ThrowArgumentParseFailed("x", args[i]); // no++
                        arg0Parsed = true;
                        continue;
                    }


                    var name = args[i];

                    switch (name)
                    {
                        case "--x":
                            if (!int.TryParse(args[++i], out arg0)) ThrowArgumentParseFailed("x", args[i]);
                            arg0Parsed = true;
                            break;
                        case "--y":
                            if (!int.TryParse(args[++i], out arg1)) ThrowArgumentParseFailed("y", args[i]);
                            arg1Parsed = true;
                            break;

                        default:
                            if (string.Equals(name, "--x", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!int.TryParse(args[++i], out arg0)) ThrowArgumentParseFailed("x", args[i]);
                                arg0Parsed = true;
                                break;
                            }
                            if (string.Equals(name, "--y", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!int.TryParse(args[++i], out arg1)) ThrowArgumentParseFailed("y", args[i]);
                                arg1Parsed = true;
                                break;
                            }

                            ThrowArgumentNameNotFound(name);
                            break;
                    }
                }
                if (!arg0Parsed) ThrowRequiredArgumentNotParsed("x");
                if (!arg1Parsed) ThrowRequiredArgumentNotParsed("y");

                command(arg0!, arg1!);
            }

            catch (Exception ex)
            {
                Environment.ExitCode = 1;
                LogError(ex.ToString());
            }
        }
    }
}


