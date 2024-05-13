using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using ConsoleAppFramework;
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

ConsoleApp.Run(args, static (int x) =>
{
    Console.WriteLine("yah:" + x);
});

namespace Takoyaki
{
    public enum MyEnum
    {

    }
}



public class MyClass
{
    /// <param name="takoyaki">--tako, -t, foo bar baz.</param>
    public void Foo(int takoyaki, int y)
    {
    }
}


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
    //[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    //public class CommandParameterAttribute : Attribute
    //{
    //    public Type ParserType { get; }

    //    public CommandParameterAttribute(Type parserType)
    //    {
    //        this.ParserType = parserType;
    //    }
    //}

    //public interface IParser<T>
    //{
    //    static abstract bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out T result);
    //}





    //internal static partial class ConsoleAqpp
    //{
    //    public static void Run2(string[] args, Action<int, global::MyClass> command)
    //    {
    //        var arg0 = default(int);
    //        var arg0Parsed = false;
    //        var arg1 = default(global::MyClass);
    //        var arg1Parsed = false;

    //        for (int i = 0; i < args.Length; i++)
    //        {
    //            var name = args[i];

    //            switch (name)
    //            {
    //                case "xxxx":
    //                    if (!int.TryParse(args[++i], out arg0)) ThrowArgumentParseFailed("xxxx", args[i]);
    //                    arg0Parsed = true;
    //                    break;
    //                case "zzz":
    //                    try { arg1 = System.Text.Json.JsonSerializer.Deserialize<global::MyClass>(args[++i]); } catch { ThrowArgumentParseFailed("zzz", args[i]); }
    //                    arg1Parsed = true;
    //                    break;

    //                default:
    //                    if (string.Equals(name, "xxxx", StringComparison.OrdinalIgnoreCase))
    //                    {
    //                        if (!int.TryParse(args[++i], out arg0)) ThrowArgumentParseFailed("xxxx", args[i]);
    //                        arg0Parsed = true;
    //                        break;
    //                    }
    //                    if (string.Equals(name, "zzz", StringComparison.OrdinalIgnoreCase))
    //                    {
    //                        try { arg1 = System.Text.Json.JsonSerializer.Deserialize<global::MyClass>(args[++i]); } catch { ThrowArgumentParseFailed("zzz", args[i]); }
    //                        arg1Parsed = true;
    //                        break;
    //                    }

    //                    ThrowInvalidArgumentName(name);
    //                    break;
    //            }
    //        }

    //        if (!arg0Parsed) ThrowRequiredArgumentNotParsed("xxxx");
    //        if (!arg1Parsed) ThrowRequiredArgumentNotParsed("zzz");

    //        command(arg0!, arg1!);
    //    }
    //}
}