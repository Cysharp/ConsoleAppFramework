using ConsoleAppFramework;
using System.ComponentModel.DataAnnotations;

// not parallel to check exit-code
[assembly: NotInParallel]

namespace ConsoleAppFramework.NativeAotTests;

public class NativeAotTest
{
    ConsoleApp.ConsoleAppBuilder app;

    public NativeAotTest()
    {
        Environment.ExitCode = 0; // reset ExitCode

        this.app = ConsoleApp.Create();

        app.UseFilter<LoggingFilter>();
        app.Add("", Commands.Root);
        app.Add("json", Commands.RecordJson);
    }

    [Test]
    public async Task RunWithFilter()
    {
        // check NativeAot trimming,command requires [DynamicDependency]
        string[] runArgs =
        [
            "input.txt",
            "--count", "3",
            "--quiet"
        ];

        await app.RunAsync(runArgs);
        await Assert.That(Environment.ExitCode).IsEqualTo(0);
    }

    [Test]
    public async Task JsonInvalid()
    {
        string[] runArgs =
        [
            "json",
            "--record",
            "{ \"X\" = 10, \"Y\" = 20 }"
        ];

        await app.RunAsync(runArgs);
        await Assert.That(Environment.ExitCode).IsNotEqualTo(0);
    }
}

internal static class Commands
{
    public static async Task<int> Root(
        [Argument] string path,
        [Range(1, 10)] int count = 1,
        bool quiet = false,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);
        if (!quiet)
        {
            Console.WriteLine($"Processing {path} with count {count}");
        }
        return 0;
    }

    public static void RecordJson(MyRecord record)
    {
        Console.WriteLine($"Record: X={record.X}, Y={record.Y}");
    }
}


internal sealed class LoggingFilter(ConsoleAppFilter next) : ConsoleAppFilter(next)
{
    public override async Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)
    {
        try
        {
            await Next.InvokeAsync(context, cancellationToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled exception: {ex.Message}");
            throw;
        }
    }
}

public record MyRecord(int X, int Y);
