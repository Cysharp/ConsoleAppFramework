using ConsoleAppFramework;
using System.ComponentModel.DataAnnotations;

var app = ConsoleApp.Create();

app.UseFilter<LoggingFilter>();
app.Add("", Commands.Root);

string[] runArgs =
[
    "input.txt",
    "--count", "3",
    "--quiet"
];

await app.RunAsync(runArgs);

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
}
