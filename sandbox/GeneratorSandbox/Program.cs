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


args = ["--x", "18", "--y", "aiueokakikukeko"]; // test.


// ConsoleApp.Run(args, Run2); void Run2(int x, int yzzzz) { };

unsafe { ConsoleApp.Run(args, &Run2); static void Run2(int x, [System.ComponentModel.DataAnnotations.Range(0, 10)]int y) { }; }

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
        //public static void ValidateParameter(object? value, ParameterInfo parameter, ValidationContext validationContext, ref StringBuilder? errorMessages)
        //{
        //    validationContext.DisplayName = parameter.Name ?? "";
        //    validationContext.Items.Clear();

        //    foreach (var validator in parameter.GetCustomAttributes<ValidationAttribute>(false))
        //    {
        //        var result = validator.GetValidationResult(value, validationContext);
        //        if (result != null)
        //        {
        //            if (errorMessages == null)
        //            {
        //                errorMessages = new StringBuilder();
        //            }
        //            errorMessages.AppendLine(result.ErrorMessage);
        //        }
        //    }
        //}


        static Action<string>? logErrorAction2;
        public static Action<string> LogError2
        {
            get => logErrorAction2 ??= (static msg => Log(msg));
            set => logErrorAction2 = value;
        }


        // [MethodImpl
        public static void Run2(string[] args, Action<int, int> command)
        {

            // command.Method


            if (TryShowHelpOrVersion(args)) return;

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

                var validationContext = new ValidationContext(1000, null, null);
                var parameters = command.GetMethodInfo().GetParameters();
                StringBuilder? errorMessages = null;
                ValidateParameter(arg0, parameters[0], validationContext, ref errorMessages);
                ValidateParameter(arg1, parameters[1], validationContext, ref errorMessages);
                if (errorMessages != null)
                {
                    throw new ValidationException(errorMessages.ToString());
                }

                command(arg0!, arg1!);
            }
            catch (Exception ex)
            {
                Environment.ExitCode = 1;
                if (ex is ValidationException ve)
                {
                    LogError(ex.Message);
                }
                else
                {
                    LogError(ex.ToString());
                }
            }
        }
    }
}


