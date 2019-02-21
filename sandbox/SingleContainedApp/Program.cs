using MicroBatchFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

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
}

public class Foo : BatchBase
{
    public void Echo(string msg)
    {
        this.Context.Logger.LogInformation(msg);
    }

    public void Sum(int x, int y)
    {
        this.Context.Logger.LogInformation((x + y).ToString());
    }
}

public class Bar : BatchBase
{
    public void Hello2()
    {
        this.Context.Logger.LogInformation("H E L L O");
    }
}



namespace SingleContainedApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new HostBuilder()
                .ConfigureLogging(x => x.AddConsole())
                .RunBatchEngine(args, new batchInterceptor()); // don't pass <T>.
        }
    }

    public class batchInterceptor : IBatchInterceptor
    {
        Stopwatch start;

        public ValueTask OnBatchEngineBegin()
        {
            start = Stopwatch.StartNew();
            return default(ValueTask);
        }

        public ValueTask OnBatchEngineEnd()
        {
            Console.WriteLine(start.Elapsed.TotalMilliseconds + "ms");
            return default(ValueTask);
        }

        public ValueTask OnBatchRunBegin(BatchContext context)
        {
            Console.WriteLine("Yeah3");
            return default(ValueTask);
        }

        public ValueTask OnBatchRunComplete(BatchContext context, string errorMessageIfFailed, Exception exceptionIfExists)
        {
            Console.WriteLine("Yeah4");
            return default(ValueTask);
        }
    }

    //public class MyClass : BatchBase
    //{
    //    public async Task Run(int foo,
    //        [Option("b", "Bar is Bar.")]string bar,
    //        bool tako = false)
    //    {
    //        logger.LogInformation("Yeah!" + (foo, bar, tako));

    //        await Task.Delay(TimeSpan.FromSeconds(1), Context.CancellationToken);

    //        logger.LogDebug("END");
    //    }

    //    public void NoRun(string foo)
    //    {
    //        logger.LogInformation("OHHHHH:" + foo);
    //    }

    //    public void VoidRun()
    //    {
    //        logger.LogInformation("VOOOO");
    //    }
    //}
}
