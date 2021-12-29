using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using Xunit;

namespace ConsoleAppFramework.Tests
{
    public class CommandAttributeTest
    {
        class CommandAttributeTestCommand : ConsoleAppBase
        {
            ResultContainer _Result;
            public CommandAttributeTestCommand(ResultContainer r)
            {
                _Result = r;
            }
            [Command("test")]
            public void TestCommand(int value)
            {
                _Result.X = value;
            }
        }
        class ResultContainer
        {
            public int X;
        }
        //[Fact]
        //public async Task TestCommandName()
        //{
        //    var host = Host.CreateDefaultBuilder()
        //        .ConfigureServices((c, services) =>
        //        {
        //            services.AddSingleton<ResultContainer>();
        //        })
        //        .UseConsoleAppFramework<CommandAttributeTestCommand>(new string[]{ "test", "-value", "1" })
        //        .Build();
        //    var result = host.Services.GetService<ResultContainer>();
        //    await host.RunAsync();
        //    result.X.Should().Be(1);
        //}

    }
}