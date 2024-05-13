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

args = ["--x", "10", "--y", "20"]; // test.


// var s = "foo";
// s.AsSpan().Split(',',).


// BigInteger.TryParse(
// Version.TryParse(

// Uri.TryCreate(UriCreationOptions


// IParsable<Complex>.TryParse(

// --x


ConsoleApp.Run(args, Command.Execute);


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

//await ConsoleApp.RunAsync(args, static async (int x, [FromServices] MyClass mc, CancellationToken cancellationToken) =>
//{
//    Console.WriteLine((x, mc));
//    await Task.Yield();
//    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
//    Console.WriteLine("end");
//});


//ConsoleApp.Run(args, &Methods.Method);

static void Foo(RunRun rrrr)
{
}


internal delegate void RunRun(int x, int y = 100);


public static class Command
{
    /// <summary>
    /// Fuga Fuga
    /// </summary>
    /// <param name="hello">-h, left x</param>
    /// <param name="world">-w|--woorong, world</param>
    public static void Execute(int hello, int world = 12345, CancellationToken cancellationToken = default)
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
    partial class ConsoleApp
    {
        public static async void Run2(string[] args, Action<int, int, global::System.Threading.CancellationToken> command)
        {
            using var posixSignalHandler = PosixSignalHandler.Register();
            var arg0 = default(int);
            var arg0Parsed = false;
            var arg1 = (int)12345;
            var arg2 = posixSignalHandler.Token;

            for (int i = 0; i < args.Length; i++)
            {
                var name = args[i];

                switch (name)
                {
                    case "--hello":
                    case "-h":
                        if (!int.TryParse(args[++i], out arg0)) ThrowArgumentParseFailed("hello", args[i]);
                        arg0Parsed = true;
                        break;
                    case "--world":
                    case "-w":
                    case "--woorong":
                        if (!int.TryParse(args[++i], out arg1)) ThrowArgumentParseFailed("world", args[i]);
                        break;

                    default:
                        if (string.Equals(name, "--hello", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(name, "-h", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!int.TryParse(args[++i], out arg0)) ThrowArgumentParseFailed("hello", args[i]);
                            arg0Parsed = true;
                            break;
                        }
                        if (string.Equals(name, "--world", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(name, "-w", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(name, "--woorong", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!int.TryParse(args[++i], out arg1)) ThrowArgumentParseFailed("world", args[i]);
                            break;
                        }

                        ThrowArgumentNameNotFound(name);
                        break;
                }
            }

            if (!arg0Parsed) ThrowRequiredArgumentNotParsed("hello");


            //posixSignalHandler.

            try
            {
                var commandRun = Task.Run(() => command(arg0!, arg1!, arg2!));
                var t = await Task.WhenAny(commandRun, new PosixSignalHandler2().TimeoutAfterCanceled);
                if (t != commandRun) // success
                {
                    // Timeout.

                }
            }
            catch (OperationCanceledException ex) when (ex.CancellationToken == posixSignalHandler.Token) { }

        }
    }

    sealed class PosixSignalHandler2 : IDisposable
    {
        public CancellationToken Token => cancellationTokenSource.Token;
        public Task TimeoutAfterCanceled => timeoutTask.Task;
        public TimeSpan Timeout;

        CancellationTokenSource cancellationTokenSource;
        CancellationTokenSource? timeoutCancellationTokenSource;
        TaskCompletionSource timeoutTask;

        PosixSignalRegistration? sigInt;
        PosixSignalRegistration? sigQuit;
        PosixSignalRegistration? sigTerm;

        public PosixSignalHandler2()
        {
            cancellationTokenSource = new CancellationTokenSource();
            timeoutTask = new();
        }

        public static PosixSignalHandler2 Register()
        {
            var handler = new PosixSignalHandler2();

            Action<PosixSignalContext> handleSignal = handler.HandlePosixSignal;

            handler.sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, handleSignal);
            handler.sigQuit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, handleSignal);
            handler.sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, handleSignal);

            return handler;
        }

        async void HandlePosixSignal(PosixSignalContext context)
        {
            context.Cancel = true;
            cancellationTokenSource?.Cancel();
            timeoutCancellationTokenSource = new CancellationTokenSource();
            try
            {
                await Task.Delay(Timeout, timeoutCancellationTokenSource.Token);
            }
            catch (OperationCanceledException) { }
            timeoutTask.TrySetResult();
        }

        public void Dispose()
        {
            sigInt?.Dispose();
            sigQuit?.Dispose();
            sigTerm?.Dispose();
            timeoutCancellationTokenSource?.Cancel();
        }
    }
}

