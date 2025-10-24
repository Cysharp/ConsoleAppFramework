using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// fail
//await ConsoleApp.RunAsync(args, Commands.Save);



var app = ConsoleApp.Create();


args = ["--x", "10", "--y", "20", "-f", "Orange", "-v", "--prefix-output", "takoyakix"];


// Enum.TryParse<Fruit>("", true,
// parse immediately

var f = app.AddGlobalOption<Fruit>(ref args, "-f");



var verbose = app.AddGlobalOption<bool>(ref args, "-v|--verbose");
var noColor = app.AddGlobalOption<bool>(ref args, "--no-color", "Don't colorize output.");
var dryRun = app.AddGlobalOption<bool>(ref args, "--dry-run");
var prefixOutput = app.AddRequiredGlobalOption<string>(ref args, "--prefix-output|-pp|-po", "Prefix output with level.");

app.ConfigureServices(x =>
{
    // to use command body
    x.AddSingleton<GlobalOptions>(new GlobalOptions(verbose, noColor, dryRun, prefixOutput));

    // variable for setup other DI
    x.AddLogging(l =>
    {
        var console = l.AddSimpleConsole();
        if (verbose)
        {
            console.SetMinimumLevel(LogLevel.Trace);
        }
    });
});

app.Add<Commands>("");

app.Run(args);


static T ParseArgumentEnum<T>(ref string[] args, int i)
    where T : struct, Enum
{
    if ((i + 1) < args.Length)
    {
        if (Enum.TryParse<T>(args[i + 1], out var value))
        {
            //RemoveRange(ref args, i, 2);
            return value;
        }

        //ThrowArgumentParseFailed(args[i], args[i + 1]);
    }
    else
    {
        // ThrowArgumentParseFailed(args[i], "");
    }

    return default;
}

public record GlobalOptions(bool Verbose, bool NoColor, bool DryRun, string PrefixOutput);


public enum Fruit
{
    Orange, Apple, Grape
}


public class Commands(GlobalOptions globalOptions)
{
    /// <summary>
    /// Some sort of save command.
    /// </summary>
    public async Task<int> Save(int x, int y)
    {
        Console.WriteLine(globalOptions);
        Console.WriteLine("Called this:" + new { x, y });
        await Task.Delay(1000);
        return 0;
    }
}

public class Teset
{

    static bool TryParse<T>(string s, out T result)
    {
        if (typeof(T) == typeof(string))
        {
            result = Unsafe.As<string, T>(ref s);
            return true;
        }
        else if (typeof(T) == typeof(char))
        {
            if (char.TryParse(s, out var v))
            {
                result = Unsafe.As<char, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(sbyte))
        {
            if (sbyte.TryParse(s, out var v))
            {
                result = Unsafe.As<sbyte, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(byte))
        {
            if (byte.TryParse(s, out var v))
            {
                result = Unsafe.As<byte, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(short))
        {
            if (short.TryParse(s, out var v))
            {
                result = Unsafe.As<short, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(ushort))
        {
            if (ushort.TryParse(s, out var v))
            {
                result = Unsafe.As<ushort, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(int))
        {
            if (int.TryParse(s, out var v))
            {
                result = Unsafe.As<int, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(long))
        {
            if (long.TryParse(s, out var v))
            {
                result = Unsafe.As<long, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(uint))
        {
            if (uint.TryParse(s, out var v))
            {
                result = Unsafe.As<uint, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(ulong))
        {
            if (ulong.TryParse(s, out var v))
            {
                result = Unsafe.As<ulong, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(decimal))
        {
            if (decimal.TryParse(s, out var v))
            {
                result = Unsafe.As<decimal, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(float))
        {
            if (float.TryParse(s, out var v))
            {
                result = Unsafe.As<float, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(double))
        {
            if (double.TryParse(s, out var v))
            {
                result = Unsafe.As<double, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(DateTime))
        {
            if (DateTime.TryParse(s, out var v))
            {
                result = Unsafe.As<DateTime, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(DateTimeOffset))
        {
            if (DateTimeOffset.TryParse(s, out var v))
            {
                result = Unsafe.As<DateTimeOffset, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(TimeOnly))
        {
            if (TimeOnly.TryParse(s, out var v))
            {
                result = Unsafe.As<TimeOnly, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(DateOnly))
        {
            if (DateOnly.TryParse(s, out var v))
            {
                result = Unsafe.As<DateOnly, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(Version))
        {
            if (Version.TryParse(s, out var v))
            {
                result = Unsafe.As<Version, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else if (typeof(T) == typeof(Guid))
        {
            if (Guid.TryParse(s, out var v))
            {
                result = Unsafe.As<Guid, T>(ref v);
                return true;
            }
            result = default;
            return false;
        }
        else
        {
            if (typeof(T).IsEnum)
            {
                if (Enum.TryParse(typeof(T), s, ignoreCase: true, out var v))
                {
                    result = (T)v;
                    return true;
                }
            }
            result = default;
            return false;
        }
    }
}

//   case SpecialType.System_String:
//    // no parse
//    if (increment)
//    {
//        return $"if (!TryIncrementIndex(ref i, commandArgs.Length)) {{ ThrowArgumentParseFailed(\"{argumentName}\", commandArgs[i]); }} else {{ arg{argCount} = commandArgs[i]; }}";
//    }
//    else
//    {
//        return $"arg{argCount} = commandArgs[i];";
//    }

//case SpecialType.System_Boolean:
//    return $"arg{argCount} = true;"; // bool is true flag
//case SpecialType.System_Char:
//case SpecialType.System_SByte:
//case SpecialType.System_Byte:
//case SpecialType.System_Int16:
//case SpecialType.System_UInt16:
//case SpecialType.System_Int32:
//case SpecialType.System_UInt32:
//case SpecialType.System_Int64:
//case SpecialType.System_UInt64:
//case SpecialType.System_Decimal:
//case SpecialType.System_Single:
//case SpecialType.System_Double:
//case SpecialType.System_DateTime:
//    tryParseKnownPrimitive = true;
//    break;
//default:
//    // Enum
//    if (type.TypeKind == TypeKind.Enum)
//    {
//        return $"if ({incrementIndex}!Enum.TryParse<{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>(commandArgs[i], true, {outArgVar})) {{ ThrowArgumentParseFailed(\"{argumentName}\", commandArgs[i]); }}{elseExpr}";
//    }

//    // ParamsArray
//    if (IsParams)
//    {
//        return $"{(increment ? "i++; " : "")}if (!TryParseParamsArray(commandArgs, ref arg{argCount}, ref i)) {{ ThrowArgumentParseFailed(\"{argumentName}\", commandArgs[i]); }}{elseExpr}";
//    }

//    // Array
//    if (type.TypeKind == TypeKind.Array)
//    {
//        var elementType = (type as IArrayTypeSymbol)!.ElementType;
//        var parsable = WellKnownTypes.Value.ISpanParsable;
//        if (parsable != null) // has parsable
//        {
//            if (elementType.AllInterfaces.Any(x => x.EqualsUnconstructedGenericType(parsable)))
//            {
//                return $"if ({incrementIndex}!TrySplitParse(commandArgs[i], {outArgVar})) {{ ThrowArgumentParseFailed(\"{argumentName}\", commandArgs[i]); }}{elseExpr}";
//            }
//        }
//        break;
//    }

//    // System.DateTimeOffset, System.Guid,  System.Version
//    tryParseKnownPrimitive = WellKnownTypes.Value.HasTryParse(type);

// `using var posixSignalHandler = PosixSignalHandler.Register(Timeout);`

//namespace ConsoleAppFramework
//{
//    internal static partial class ConsoleApp
//    {
//        internal partial class ConsoleAppBuilder
//        {
//            public T AddGlobalOption<T>(ref string[] args, [ConstantExpected] string name, [ConstantExpected] string description = "", T defaultValue = default(T))
//            // where T : IParsable<T>
//            {
//                var aliasCount = name.AsSpan().Count("|") + 1;
//                if (aliasCount == 1)
//                {
//                    for (int i = 0; i < args.Length; i++)
//                    {
//                        if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
//                        {
//                            return ParseArgument<T>(ref args, i);
//                        }
//                    }
//                }
//                else
//                {
//                    Span<Range> aliases = stackalloc Range[aliasCount];
//                    if (name.AsSpan().Split(aliases, '|') == 2)
//                    {
//                        var name1 = name.AsSpan()[aliases[0]].Trim();
//                        var name2 = name.AsSpan()[aliases[1]].Trim();

//                        for (int i = 0; i < args.Length; i++)
//                        {
//                            if (args[i].AsSpan().Equals(name1, StringComparison.OrdinalIgnoreCase) || args[i].AsSpan().Equals(name2, StringComparison.OrdinalIgnoreCase))
//                            {
//                                return ParseArgument<T>(ref args, i);
//                            }
//                        }
//                    }
//                    else
//                    {
//                        for (int i = 0; i < args.Length; i++)
//                        {
//                            if (Contains(name, aliases, args[i]))
//                            {
//                                return ParseArgument<T>(ref args, i);
//                            }
//                        }
//                    }
//                }

//                return defaultValue;
//            }

//            public T AddRequiredGlobalOption<T>(ref string[] args, [ConstantExpected] string name, [ConstantExpected] string description = "")
//                where T : IParsable<T>
//            {
//                if (typeof(T) == typeof(bool)) throw new InvalidOperationException("<bool> can not use in AddRequiredGlobalOption. use AddGlobalOption instead.");

//                var aliasCount = name.AsSpan().Count("|") + 1;
//                if (aliasCount == 1)
//                {
//                    for (int i = 0; i < args.Length; i++)
//                    {
//                        if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
//                        {
//                            return ParseArgument<T>(ref args, i);
//                        }
//                    }
//                }
//                else
//                {
//                    Span<Range> aliases = stackalloc Range[aliasCount];
//                    if (name.AsSpan().Split(aliases, '|') == 2)
//                    {
//                        var name1 = name.AsSpan()[aliases[0]].Trim();
//                        var name2 = name.AsSpan()[aliases[1]].Trim();

//                        for (int i = 0; i < args.Length; i++)
//                        {
//                            if (args[i].AsSpan().Equals(name1, StringComparison.OrdinalIgnoreCase) || args[i].AsSpan().Equals(name2, StringComparison.OrdinalIgnoreCase))
//                            {
//                                return ParseArgument<T>(ref args, i);
//                            }
//                        }
//                    }
//                    else
//                    {
//                        for (int i = 0; i < args.Length; i++)
//                        {
//                            if (Contains(name, aliases, args[i]))
//                            {
//                                return ParseArgument<T>(ref args, i);
//                            }
//                        }
//                    }
//                }

//                ThrowRequiredArgumentNotParsed(name);
//                return default;
//            }

//            static T ParseArgument<T>(ref string[] args, int i)
//                    where T : IParsable<T>
//            {
//                if (typeof(T) == typeof(bool))
//                {
//                    RemoveRange(ref args, i, 1);
//                    var t = true;
//                    return Unsafe.As<bool, T>(ref t);
//                }
//                else
//                {
//                    if ((i + 1) < args.Length)
//                    {
//                        if (T.TryParse(args[i + 1], null, out var value))
//                        {
//                            RemoveRange(ref args, i, 2);
//                            return value;
//                        }

//                        ThrowArgumentParseFailed(args[i], args[i + 1]);
//                    }
//                    else
//                    {
//                        ThrowArgumentParseFailed(args[i], "");
//                    }
//                }

//                return default;
//            }

//            static void RemoveRange(ref string[] args, int index, int length)
//            {
//                if (length == 0) return;

//                var temp = new string[args.Length - length];
//                Array.Copy(args, temp, index);
//                Array.Copy(args, index + length, temp, index, args.Length - index - length);

//                args = temp;
//            }

//            static bool Contains(ReadOnlySpan<char> nameToSlice, Span<Range> ranges, string target)
//            {
//                for (int i = 0; i < ranges.Length; i++)
//                {
//                    var name = nameToSlice[ranges[i]].Trim();
//                    if (name.Equals(target, StringComparison.OrdinalIgnoreCase))
//                    {
//                        return true;
//                    }
//                }
//                return false;
//            }
//        }
//    }
//}
