using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ConsoleAppFramework.Tests
{
    public class SingleContainedTest
    {
        ITestOutputHelper testOutput;

        public SingleContainedTest(ITestOutputHelper testOutput)
        {
            this.testOutput = testOutput;
        }

        public class SimpleZeroArgs : ConsoleAppBase
        {
            public void Hello()
            {
                Context.Logger.LogInformation($"ok");
            }
        }

        [Fact]
        public async Task SimpleZeroArgsTest()
        {
            var log = new LogStack();
            await new HostBuilder()
                .ConfigureTestLogging(testOutput, log, true)
                .RunConsoleAppFrameworkAsync<SimpleZeroArgs>(new string[0]);
            log.InfoLogShouldBe(0, "ok");
        }

        public class SimpleTwoArgs : ConsoleAppBase
        {
            public void Hello(
                string name,
                int repeat)
            {
                Context.Logger.LogInformation($"name:{name}");
                Context.Logger.LogInformation($"repeat:{repeat}");
            }
        }

        [Fact]
        public async Task SimpleTwoArgsTest()
        {
            {
                var args = "-name foo -repeat 3".Split(' ');
                var log = new LogStack();
                await new HostBuilder()
                    .ConfigureTestLogging(testOutput, log, true)
                    .RunConsoleAppFrameworkAsync<SimpleTwoArgs>(args);
                log.InfoLogShouldBe(0, "name:foo");
                log.InfoLogShouldBe(1, "repeat:3");
            }
            {
                //var args = "-repeat 3".Split(' ');
                //var log = new LogStack();
                //using (TextWriterBridge.BeginSetConsoleOut(testOutput, log))
                //{
                //    {
                //        await new HostBuilder()
                //            .ConfigureTestLogging(testOutput, log, true)
                //            .RunConsoleAppFrameworkAsync<SimpleTwoArgs>(args);
                //    }

                //    log.ToStringInfo().Should().Contain("Required parameter \"name\" not found in argument");
                //}
                 
            }
            {
                var log = new LogStack();
                using (TextWriterBridge.BeginSetConsoleOut(testOutput, log))
                {
                    var args = new string[0];
                    await new HostBuilder().RunConsoleAppFrameworkAsync<SimpleTwoArgs>(args);
                    log.ToStringInfo().Should().Contain("Options:"); // ok to show help
                }
            }
        }

        public class SimpleComplexArgs : ConsoleAppBase
        {
            public void Hello(
                ComplexStructure person,
                int repeat)
            {
                Context.Logger.LogInformation($"person.Age:{person.Age} person.Name:{person.Name}");
                Context.Logger.LogInformation($"repeat:{repeat}");
            }
        }

        public class ComplexStructure
        {
            public int Age { get; set; }
            public string Name { get; set; }
        }

        [Fact]
        public async Task SimpleComplexArgsTest()
        {
            {
                var args = "-person {\"Age\":10,\"Name\":\"foo\"} -repeat 3".Split(' ');
                var log = new LogStack();
                await new HostBuilder()
                    .ConfigureTestLogging(testOutput, log, true)
                    .RunConsoleAppFrameworkAsync<SimpleComplexArgs>(args);
                log.InfoLogShouldBe(0, "person.Age:10 person.Name:foo");
                log.InfoLogShouldBe(1, "repeat:3");
            }
        }

        public class TwoArgsWithOption : ConsoleAppBase
        {
            public void Hello(
                [Option("n", "name of this")]string name,
                [Option("r", "repeat msg")]int repeat)
            {
                Context.Logger.LogInformation($"name:{name}");
                Context.Logger.LogInformation($"repeat:{repeat}");
            }
        }

        [Fact]
        public async Task TwoArgsWithOptionTest()
        {
            {
                var args = "-n foo -r 3".Split(' ');
                var log = new LogStack();
                await new HostBuilder()
                    .ConfigureTestLogging(testOutput, log, true)
                    .RunConsoleAppFrameworkAsync<TwoArgsWithOption>(args);
                log.InfoLogShouldBe(0, "name:foo");
                log.InfoLogShouldBe(1, "repeat:3");
            }
            {
                var log = new LogStack();
                using (TextWriterBridge.BeginSetConsoleOut(testOutput, log))
                {
                    var args = new string[0];
                    await new HostBuilder().RunConsoleAppFrameworkAsync<TwoArgsWithOption>(args);
                    var strAssertion = log.ToStringInfo().Should();
                    strAssertion.Contain("Options:"); // ok to show help
                    strAssertion.Contain("-n");
                    strAssertion.Contain("name of this");
                    strAssertion.Contain("-r");
                    strAssertion.Contain("repeat msg");
                }
            }
        }

        public class TwoArgsWithDefault : ConsoleAppBase
        {
            public void Hello(string name, int repeat = 100, string hoo = null)
            {
                Context.Logger.LogInformation($"name:{name}");
                Context.Logger.LogInformation($"repeat:{repeat}");
                Context.Logger.LogInformation($"hoo:{hoo}");
            }
        }

        [Fact]
        public async Task TwoArgsWithDefaultTest()
        {
            {
                var args = "-name foo".Split(' ');
                var log = new LogStack();
                await new HostBuilder()
                    .ConfigureTestLogging(testOutput, log, true)
                    .RunConsoleAppFrameworkAsync<TwoArgsWithDefault>(args);
                log.InfoLogShouldBe(0, "name:foo");
                log.InfoLogShouldBe(1, "repeat:100");
                log.InfoLogShouldBe(2, "hoo:");
            }
        }

        public class AllDefaultParameters : ConsoleAppBase
        {
            public void Hello(string name = "aaa", int repeat = 100, string hoo = null)
            {
                Context.Logger.LogInformation($"name:{name}");
                Context.Logger.LogInformation($"repeat:{repeat}");
                Context.Logger.LogInformation($"hoo:{hoo}");
            }
        }

        [Fact]
        public async Task AllDefaultParametersTest()
        {
            {
                var args = new string[0];
                var log = new LogStack();
                await new HostBuilder()
                    .ConfigureTestLogging(testOutput, log, true)
                    .RunConsoleAppFrameworkAsync<AllDefaultParameters>(args);
                log.InfoLogShouldBe(0, "name:aaa");
                log.InfoLogShouldBe(1, "repeat:100");
                log.InfoLogShouldBe(2, "hoo:");
            }
        }

        public class BooleanSwitch : ConsoleAppBase
        {
            public void Hello(string x, bool bar, bool foo = false, bool yeah = false)
            {
                Context.Logger.LogInformation($"x:{x}");
                Context.Logger.LogInformation($"bar:{bar}");
                Context.Logger.LogInformation($"foo:{foo}");
                Context.Logger.LogInformation($"yeah:{yeah}");
            }
        }

        [Fact]
        public async Task BooleanSwitchTest()
        {
            {
                var log = new LogStack();
                var args = "-x foo -bar -foo -yeah".Split(' ');
                await new HostBuilder()
                    .ConfigureTestLogging(testOutput, log, true)
                    .RunConsoleAppFrameworkAsync<BooleanSwitch>(args);
                log.InfoLogShouldBe(0, "x:foo");
                log.InfoLogShouldBe(1, "bar:True");
                log.InfoLogShouldBe(2, "foo:True");
                log.InfoLogShouldBe(3, "yeah:True");
            }
            {
                var log = new LogStack();
                var args = "-x foo -bar -foo".Split(' ');
                await new HostBuilder()
                    .ConfigureTestLogging(testOutput, log, true)
                    .RunConsoleAppFrameworkAsync<BooleanSwitch>(args);
                log.InfoLogShouldBe(0, "x:foo");
                log.InfoLogShouldBe(1, "bar:True");
                log.InfoLogShouldBe(2, "foo:True");
                log.InfoLogShouldBe(3, "yeah:False");
            }
            {
                var log = new LogStack();
                var args = "-x foo -foo -yeah".Split(' ');
                await new HostBuilder()
                    .ConfigureTestLogging(testOutput, log, true)
                    .RunConsoleAppFrameworkAsync<BooleanSwitch>(args);
                log.InfoLogShouldBe(0, "x:foo");
                log.InfoLogShouldBe(1, "bar:False");
                log.InfoLogShouldBe(2, "foo:True");
                log.InfoLogShouldBe(3, "yeah:True");
            }
        }
    }
}
