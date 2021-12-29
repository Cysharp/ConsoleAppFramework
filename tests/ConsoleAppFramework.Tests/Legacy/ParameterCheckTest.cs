using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ConsoleAppFramework.Tests
{
    public class ParameterCheckTest
    {
        ITestOutputHelper testOutput;

        public ParameterCheckTest(ITestOutputHelper testOutput)
        {
            this.testOutput = testOutput;
        }

        public class DictionaryCheck : ConsoleAppBase
        {
            public void Hello(Dictionary<string, string> q)
            {

                foreach (var item in q)
                {
                    Context.Logger.LogInformation($"{item.Key}:{item.Value}");
                }
            }
        }

        [Fact]
        public async Task DictParse()
        {
            var args = @"-q {""Key1"":""Value1*""}".Split(' ');

            var log = new LogStack();
            await new HostBuilder()
                .ConfigureTestLogging(testOutput, log, true)
                .RunConsoleAppFrameworkAsync<DictionaryCheck>(args);

            log.InfoLogShouldBe(0, "Key1:Value1*");
        }
    }
}
