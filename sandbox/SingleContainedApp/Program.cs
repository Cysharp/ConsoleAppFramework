using MicroBatchFramework;
using MicroBatchFramework.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
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

        IOptions<MyConfig> config;
        ILogger<MyFirstBatch> logger;

        public MyFirstBatch(IOptions<MyConfig> config, ILogger<MyFirstBatch> logger)
        {
            this.config = config;
            this.logger = logger;
        }

        [Command("log")]
        public void LogWrite()
        {
            Context.Logger.LogTrace("t r a c e");
            Context.Logger.LogDebug("d e b u g");
            Context.Logger.LogInformation("i n f o");
            Context.Logger.LogCritical("c r i t i c a l");
            Context.Logger.LogWarning("w a r n");
            Context.Logger.LogError("e r r o r");
        }

        [Command("opt")]
        public void ShowOption()
        {
            Console.WriteLine(config.Value.Bar);
            Console.WriteLine(config.Value.Foo);
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

    public class MyConfig
    {
        public int Foo { get; set; }
        public bool Bar { get; set; }
    }

    public class OverrideCheck : BatchBase
    {
        [Command("encode", "encode input string to base64url")]
        public void Encode([Option(0)]string input) => Console.WriteLine((input));

        [Command("decode", "decode input base64url to string")]
        public void Decode([Option(0)]string input) => Console.WriteLine((input));

        [Command("escape", "escape base64 to base64url")]
        public void Escape([Option(0)]string input) => Console.WriteLine((input));

        [Command(new[] { "unescape", "-h" }, "unescape base64url to base64")]
        public void Unescape([Option(0)]string input) => Console.WriteLine((input));

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
            await BatchHost.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    // mapping config json to IOption<MyConfig>
                    services.Configure<MyConfig>(hostContext.Configuration);
                })
                .RunBatchEngineAsync<MyFirstBatch>(args);
        }
    }
}
