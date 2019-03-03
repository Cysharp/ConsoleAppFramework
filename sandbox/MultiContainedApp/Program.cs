using MicroBatchFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MultiContainedApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await new HostBuilder().RunBatchEngineAsync(args);
        }
    }

    public class Foo : BatchBase
    {
        public void Echo(string msg)
        {
            this.Context.Logger.LogInformation(msg);
        }

        public void Sum([Option(0)]int x, [Option(1)]int y)
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
}
