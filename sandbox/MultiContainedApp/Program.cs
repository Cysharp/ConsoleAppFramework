using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MultiContainedApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //args = new string[] { "Bar.Hello3", "2" };
            //args = new string[] { "bar", "hello3", "-help" };
            //args = new string[] { "foo", "echo", "help"};
            //args = new string[] { "bar.hello2", "help" };

            await Host.CreateDefaultBuilder()
                .ConfigureLogging(x => x.SetMinimumLevel(LogLevel.Trace))
                .RunConsoleAppFrameworkAsync(args, options: new ConsoleAppOptions
                {
                    GlobalFilters = new ConsoleAppFilter[] { new MyFilter2 { Order = -1 }, new MyFilter() }
                });



            //await Host.CreateDefaultBuilder()
            //    .ConfigureLogging(x =>
            //    {
            //        x.ClearProviders();
            //        x.SetMinimumLevel(LogLevel.Trace);

            //    })
            //    .RunConsoleAppFrameworkAsync(args);
        }
    }

    [ConsoleAppFilter(typeof(MyFilter2), Order = 9999)]
    [ConsoleAppFilter(typeof(MyFilter2), Order = 9999)]
    public class Foo : ConsoleAppBase
    {
        [Command("ec", "My echo")]
        public void Echo(string msg)
        {
            Console.WriteLine(msg);
        }

        public void Sum(int x, int y)
        {
            Console.WriteLine((x + y).ToString());
        }
    }

    public class Bar : ConsoleAppBase
    {
        public void Hello2()
        {
            Console.WriteLine("H E L L O");
        }
    }



    //public class Foo : ConsoleAppBase
    //{
    //    [Command(new[] { "eo", "t" }, "Echo message to the logger")]
    //    [ConsoleAppFilter(typeof(EchoFilter), Order = 10000)]
    //    public void Echo([Option("msg", "Message to send.")]string msg)
    //    {
    //        // Console.WriteLine(new StackTrace().ToString());
    //        this.Context.Logger.LogInformation(msg);
    //    }

    //    [Command("s")]
    //    public void Sum([Option(0)]int x, [Option(1)]int y)
    //    {
    //        this.Context.Logger.LogInformation((x + y).ToString());
    //    }
    //}

    //public class Bar : ConsoleAppBase
    //{
    //    public void Hello2()
    //    {
    //        this.Context.Logger.LogInformation("H E L L O");
    //    }

    //    public void Hello3([Option(0)]int aaa)
    //    {
    //        this.Context.Logger.LogInformation("H E L L O:" + aaa);
    //    }
    //}

    public class MyFilter : ConsoleAppFilter
    {
        public async override ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next)
        {
            Console.WriteLine("call second");
            await next(context);
        }
    }

    public class MyFilter2 : ConsoleAppFilter
    {
        public async override ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next)
        {
            Console.WriteLine("call first");
            await next(context);
        }
    }

    public class EchoFilter : ConsoleAppFilter
    {
        public async override ValueTask Invoke(ConsoleAppContext context, Func<ConsoleAppContext, ValueTask> next)
        {
            Console.WriteLine("call ec");
            await next(context);
        }
    }
}
