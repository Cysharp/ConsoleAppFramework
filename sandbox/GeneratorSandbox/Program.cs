using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;


args = ["foo", "--x", "1"]; // test.


// ConsoleApp.Run(args, Run2); void Run2(int x, int yzzzz) { };


var builder = ConsoleApp.CreateBuilder();
builder.Add("foo", (int x, int y) => { });
builder.Run(args);




// var s = "foo";
// s.AsSpan().Split(',',).


// BigInteger.TryParse(
// Version.TryParse(

// Uri.TryCreate(UriCreationOptions


// IParsable<Complex>.TryParse(

// --x


static async Task<int> RunRun(int? x = null, string? y = null)
{
    await Task.Yield();
    Console.WriteLine("Hello World!" + x + y);
    return 0;
}



static void Tests<T>()
    where T : ISpanParsable<int>
{


}



// constructor injection!



public delegate void FooBar(int x, int y = 10);

public partial struct ConsoleAppBuilderTest
{
    public void Add(string commandName, Delegate command)
    {
        AddCore(commandName, command);
    }

    [Conditional("DEBUG")]
    public void Add<T>() { }

    [Conditional("DEBUG")]
    public void Add<T>(string commandName) { }

    public void Run(string[] args)
    {
        RunCore(args);
    }

    public Task RunAsync(string[] args)
    {
        Task? task = null;
        RunAsyncCore(args, ref task!);
        return task ?? Task.CompletedTask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    partial void AddCore(string commandName, Delegate command);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    partial void RunCore(string[] args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    partial void RunAsyncCore(string[] args, ref Task result);
}

//partial struct ConsoleAppBuilderTest
//{
//    Action<int, int> command1;

//    // root command => /
//    // sub command => foo/bar/baz
//    public void Add(string commandName, Action<int, int> command) // generate Add methods
//    {
//        // multi
//        switch (commandName)
//        {
//            case "foo":
//                this.command1 = command;
//                break;
//        }
//    }

//    // or RunAsync
//    public partial void Run(string[] args) // generate body
//    {
//        // --help?

//        switch (args[0])
//        {
//            case "foo":
//                RunCommand1(args.AsSpan(1), command1);
//                break;
//            case "bar":

//                break;
//        }
//    }

//    public partial Task RunAsync(string[] args) => throw new NotImplementedException();

//    // generate both invoke and invokeasync? detect which calls?
//    // void Invoke
//    static void RunCommand1(Span<string> args, Action<int, int> command)
//    {
//        // call generated...
//    }
//}



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
    }
}


