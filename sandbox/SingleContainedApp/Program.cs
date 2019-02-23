using MicroBatchFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            await new HostBuilder().RunBatchEngineAsync<MyFirstBatch>(args);
        }
    }
}
