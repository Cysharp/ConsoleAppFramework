using MicroBatchFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SingleContainedApp
{
    public class MyFirstBatch : BatchBase
    {
        public void Hello(
            [Option("n", "name of send user.")]string name,
            [Option("r", "repeat count.")]int repeat = 3)
        {
            for (int i = 0; i < repeat; i++)
            {
                this.Context.Logger.LogInformation($"Hello My Batch from {name}");
            }
        }

        [Command("version", "yeah, go!")]
        public void ShowVersion()
        {
            var version = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyFileVersionAttribute>()
                .Version;
            Console.WriteLine(version);
        }

        [Command("escape")]
        public void UrlEscape([Option(0)]string input)
        {
            Console.WriteLine(Uri.EscapeDataString(input));
        }

        [Command("timer")]
        public async Task Timer([Option(0)]uint waitSeconds)
        {
            Console.WriteLine(waitSeconds + " seconds");
            while (waitSeconds != 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), Context.CancellationToken);
                waitSeconds--;
                Console.WriteLine(waitSeconds + " seconds");
            }
        }
    }

    public class OverrideCheck : BatchBase
    {
        [Command(new[] { "help", "-h", "-help", "--help" }, "show help")]
        public void Help()
        {
            Console.WriteLine("Usage: base64urls [-version] [-help] [decode|encode|escape|unescape] [args]");
            Console.WriteLine("E.g., run this: base64urls decode QyMgaXMgYXdlc29tZQ==");
            Console.WriteLine("E.g., run this: base64urls encode \"C# is awesome.\"");
            Console.WriteLine("E.g., run this: base64urls escape \"This+is/goingto+escape==\"");
            Console.WriteLine("E.g., run this: base64urls unescape \"This-is_goingto-escape\"");
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            args = new string[] { };
            await new HostBuilder().RunBatchEngineAsync<OverrideCheck>(args);
        }
    }
}
