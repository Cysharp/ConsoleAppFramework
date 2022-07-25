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

            // can not execute #shoganai 
            // console.Output.Should().Contain("Hello help");
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

            // NOTE: Currently, ConsoleAppFramework treats the first argument as special. If the argument is '-help', it is same as '-help' option.
            //console.Output.Should().Contain("Hello -version");
        }

        [Fact]
        public void NoOptions_OneRequiredArg_Version()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "version" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_NoOptions_OneRequiredArg>(args);
            console.Output.Should().MatchRegex(@"\d.\d.\d"); // NOTE: When running with unit test runner, it returns a version of the runner.

            // NOTE: Currently, ConsoleAppFramework treats the first argument as special. If the argument is '-help', it is same as '-help' option.
            //console.Output.Should().Contain("Hello -version");
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
            
            // can not execute #shoganai 
            // console.Output.Should().Contain("Hello help");
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
            var args = new[] { "help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_NoOptions_OneOptionalArgs>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Arguments:");

            // NOTE: Currently, ConsoleAppFramework treats the first argument as special. If the argument is '-help', it is same as '-help' option.
            //console.Output.Should().Contain("Hello -help");
        }

        [Fact]
        public void NoOptions_OneOptionalArg_Version()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "version" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_NoOptions_OneOptionalArgs>(args);
            console.Output.Should().MatchRegex(@"\d.\d.\d"); // NOTE: When running with unit test runner, it returns a version of the runner.

            // NOTE: Currently, ConsoleAppFramework treats the first argument as special. If the argument is '-help', it is same as '-help' option.
            //console.Output.Should().Contain("Hello -version");
        }

        public class CommandTests_Single_NoOptions_OneOptionalArgs : ConsoleAppBase
        {
            public void Hello([Option(0)]string name = "Anonymous") => Console.WriteLine($"Hello {name}");
        }

        [Fact]
        public void CommandTests_Single_DateTimeOption_WithDoubleQuote()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "\"2022-07-01\"" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_DateTimeOption>(args);
            console.Output.Trim().Should().Be(@"2022-07-01");
        }

        [Fact]
        public void CommandTests_Single_DateTimeOption_WithoutDoubleQuote()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "2022-07-01" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_DateTimeOption>(args);
            console.Output.Trim().Should().Be(@"2022-07-01");
        }

        public class CommandTests_Single_DateTimeOption: ConsoleAppBase
        {
            public void Hello([Option(0)]DateTime dt) => Console.WriteLine($"{dt:yyyy-MM-dd}");
        }
    }
}
