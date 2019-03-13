using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MicroBatchFramework.Tests
{

    public class SubCommandTest
    {
        readonly ITestOutputHelper testOutput;

        public SubCommandTest(ITestOutputHelper testOutput)
        {
            this.testOutput = testOutput;
        }

        public class NotFoundPath : BatchBase
        {
            [Command("run")]
            public void Run(string path, string pfx, string thumbnail, string output, bool allowoverwrite = false)
            {
                Context.Logger.LogInformation($"path:{path}");
                Context.Logger.LogInformation($"pfx:{pfx}");
                Context.Logger.LogInformation($"thumbnail:{thumbnail}");
                Context.Logger.LogInformation($"output:{output}");
                Context.Logger.LogInformation($"allowoverwrite:{allowoverwrite}");
            }
        }

        [Fact]
        public async Task NotFoundPathTest()
        {
            var args = "run -path -pfx test.pfx -thumbnail 123456 -output output.csproj -allowoverwrite".Split(' ');
            var log = new LogStack();

            await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                await new HostBuilder()
                    .ConfigureTestLogging(testOutput, log, true)
                    .RunBatchEngineAsync<NotFoundPath>(args);
            });
        }
    }
}
