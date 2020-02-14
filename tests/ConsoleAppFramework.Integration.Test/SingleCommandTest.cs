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
            console.Output.Should().Contain("Hello");
        }

        public class CommandTests_Single_NoOptions_NoArgs : ConsoleAppBase
        {
            public void Hello() => Console.WriteLine("Hello");
        }
    }
}
