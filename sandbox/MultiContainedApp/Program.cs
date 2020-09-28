using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MultiContainedApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // args = new string[] { "Bar.Hello3", "2" };
            //args = new string[] { "bar", "hello3", "-help" };
            args = new string[] { "foo.eo", "-msg", "e" };
            //args = new string[] { "bar.hello2", "help" };

            await Host.CreateDefaultBuilder()
                .ConfigureLogging(x => x.SetMinimumLevel(LogLevel.Trace))
                .RunConsoleAppFrameworkAsync(args, options: new ConsoleAppFrameworkOptions
                {
                    GlobalFilters = new IConsoleAppFrameworkFilter[] { new MyFilter2 { Order = -1 }, new MyFilter() }
                });
        }
    }

    public class Foo : ConsoleAppBase
    {
        [Command(new[] { "eo", "t" }, "Echo message to the logger")]
        [ConsoleAppFrameworkFilter(typeof(EchoFilter), Order = 10)]
        public void Echo([Option("msg", "Message to send.")]string msg)
        {

            this.Context.Logger.LogInformation(msg);
        }

        [Command("s")]
        public void Sum([Option(0)]int x, [Option(1)]int y)
        {
            this.Context.Logger.LogInformation((x + y).ToString());
        }
    }

    public class Bar : ConsoleAppBase
    {
        public void Hello2()
        {
            this.Context.Logger.LogInformation("H E L L O");
        }

        public void Hello3([Option(0)]int aaa)
        {
            this.Context.Logger.LogInformation("H E L L O:" + aaa);
        }
    }

    public class MyFilter : IConsoleAppFrameworkFilter
    {
        public int Order { get; set; } // order will set from attribute.

        public async ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next)
        {
            Console.WriteLine("call second");
            await next(context);
        }
    }

    public class MyFilter2 : IConsoleAppFrameworkFilter
    {
        public int Order { get; set; } // order will set from attribute.

        public async ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next)
        {
            Console.WriteLine("call first");
            await next(context);
        }
    }

    public class EchoFilter : IConsoleAppFrameworkFilter
    {
        public int Order { get; set; }

        public async ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next)
        {
            await next(context);
        }
    }
}
