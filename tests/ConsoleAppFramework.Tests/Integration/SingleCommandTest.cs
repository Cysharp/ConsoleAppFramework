using System;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Xunit;

// ReSharper disable InconsistentNaming

namespace ConsoleAppFramework.Integration.Test
{
    public partial class SingleCommandTest
    {
        [Fact]
        public void NoOptions_NoArgs()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_NoOptions_NoArgs>(args);
            console.Output.Should().Contain("HelloMyWorld");
        }

        [Fact]
        public void IntArguments()
        {
            using var console = new CaptureConsoleOutput();

            var args = "--foo 1,2,3".Split(' ');

            ConsoleApp.RunAsync(args, (int[] foo) =>
            {
                foreach (var item in foo)
                {
                    Console.WriteLine(item);
                }
            });

            console.Output.Should().Be(@"1
2
3
");
        }

        [Fact]
        public void StringArguments()
        {
            using var console = new CaptureConsoleOutput();

            var args = "--foo a,b,c".Split(' ');

            ConsoleApp.RunAsync(args, (string[] foo) =>
            {
                foreach (var item in foo)
                {
                    Console.WriteLine(item);
                }
            });

            console.Output.Should().Be(@"a
b
c
");
        }

        public class CommandTests_Single_NoOptions_NoArgs : ConsoleAppBase
        {
            public void Hello() => Console.WriteLine("HelloMyWorld");
        }
    }
}
