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
        public void NoOptions_OneRequiredArg()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "Cysharp" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_NoOptions_OneRequiredArg>(args);
            console.Output.Should().Contain("Hello Cysharp");
        }

        [Fact]
        public void NoOptions_OneRequiredArg_ArgHelp()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_NoOptions_OneRequiredArg>(args);
            console.Output.Should().Contain("Hello help");
            // console.GetOutputText().Should().Contain("Usage:");
            // console.GetOutputText().Should().Contain("Arguments:");
        }

        [Fact]
        public void NoOptions_OneRequiredArg_Insufficient()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_NoOptions_OneRequiredArg>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Arguments:");
        }

        [Fact]
        public void NoOptions_OneRequiredArg_Help()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_NoOptions_OneRequiredArg>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Arguments:");
        }

        public class CommandTests_Single_NoOptions_OneRequiredArg : ConsoleAppBase
        {
            public void Hello([Option(0)]string name) => Console.WriteLine($"Hello {name}");
        }

        [Fact]
        public void NoOptions_OneOptionalArg()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "Cysharp" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_NoOptions_OneOptionalArgs>(args);
            console.Output.Should().Contain("Hello Cysharp");
        }

        [Fact]
        public void NoOptions_OneOptionalArg_ArgHelp()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_NoOptions_OneOptionalArgs>(args);
            console.Output.Should().Contain("Hello help");
            // console.GetOutputText().Should().Contain("Usage:");
            // console.GetOutputText().Should().Contain("Arguments:");
        }

        [Fact]
        public void NoOptions_OneOptionalArg_NoInputArg()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_NoOptions_OneOptionalArgs>(args);
            console.Output.Should().Contain("Hello Anonymous");
        }

        [Fact]
        public void NoOptions_OneOptionalArg_Help()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_NoOptions_OneOptionalArgs>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Arguments:");
        }

        public class CommandTests_Single_NoOptions_OneOptionalArgs : ConsoleAppBase
        {
            public void Hello([Option(0)]string name = "Anonymous") => Console.WriteLine($"Hello {name}");
        }
    }
}
