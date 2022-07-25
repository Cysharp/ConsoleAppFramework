using ConsoleAppFramework.Integration.Test;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ConsoleAppFramework.Tests.Integration
{
    public class HelpUsageTest
    {
        [Fact]
        public async Task ConfigureApplicationName()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { };
            await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<Runner>(args, new ConsoleAppOptions
            {
                ApplicationName = "foo"
            });
                
            var output = console.Output;

            output.Should().Contain("Usage: foo");
        }


        public class Runner : ConsoleAppBase
        {
            [Command("hello")]
            public void Hello() => Console.WriteLine("Hello");
        }
    }
}
