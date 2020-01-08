using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ConsoleAppFramework.Tests
{
    public class Multi1 : BatchBase
    {
        public void Hello1()
        {
            Context.Logger.LogInformation("ok");
        }

        public void Hello2(string input)
        {
            Context.Logger.LogInformation(input);
        }
    }

    public class Multi2 : BatchBase
    {
        public void Hello1([Option("x")]int xxx, [Option("y")]int yyy)
        {
            Context.Logger.LogInformation($"{xxx}:{yyy}");
        }

        public void Hello2(bool x, bool y, string foo, int nano = 999)
        {
            Context.Logger.LogInformation($"{x}:{y}:{foo}:{nano}");
        }
    }

    public class MultiContainedTest
    {
        ITestOutputHelper testOutput;

        public MultiContainedTest(ITestOutputHelper testOutput)
        {
            this.testOutput = testOutput;
        }

        [Fact]
        public async Task MultiContained()
        {
            {
                var args = "Multi1.Hello1".Split(' ');
                var log = new LogStack();
                await new HostBuilder()
                    .ConfigureTestLogging(testOutput, log, true)
                    .RunBatchEngineAsync(args);
                log.InfoLogShouldBe(0, "ok");
            }
            {
                var args = "Multi1.Hello2 -input yeah".Split(' ');
                var log = new LogStack();
                await new HostBuilder()
                    .ConfigureTestLogging(testOutput, log, true)
                    .RunBatchEngineAsync(args);
                log.InfoLogShouldBe(0, "yeah");
            }
            {
                var args = "Multi2.Hello1 -x 20 -y 30".Split(' ');
                var log = new LogStack();
                await new HostBuilder()
                    .ConfigureTestLogging(testOutput, log, true)
                    .RunBatchEngineAsync(args);
                log.InfoLogShouldBe(0, "20:30");
            }
            {
                var args = "Multi2.Hello2 -x -y -foo yeah".Split(' ');
                var log = new LogStack();
                await new HostBuilder()
                    .ConfigureTestLogging(testOutput, log, true)
                    .RunBatchEngineAsync(args);
                log.InfoLogShouldBe(0, "True:True:yeah:999");
            }
        }
    }
}
