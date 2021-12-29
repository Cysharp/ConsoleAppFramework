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
        public void OneRequiredOption_OneRequiredArg()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "Cysharp", "-age", "18" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_OneRequiredOption_OneRequiredArg>(args);
            console.Output.Should().Contain("Cysharp (18)");
        }

        [Fact]
        public void OneRequiredOption_OneRequiredArg_OptionLikeValueArg()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "--C--", "-age", "18" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_OneRequiredOption_OneRequiredArg>(args);
            console.Output.Should().Contain("--C-- (18)");
        }

        [Fact]
        public void OneRequiredOption_OneRequiredArg_Insufficient()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_OneRequiredOption_OneRequiredArg>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Options:");
        }

        [Fact]
        public void OneRequiredOption_OneRequiredArg_Insufficient_Options()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "Cysharp" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_OneRequiredOption_OneRequiredArg>(args);
            console.Output.Should().Contain("Required parameter \"age\"");
        }

        [Fact]
        public void OneRequiredOption_OneRequiredArg_Help()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_OneRequiredOption_OneRequiredArg>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Options:");
        }

        public class CommandTests_OneRequiredOption_OneRequiredArg : ConsoleAppBase
        {
            public void Hello([Option(0)]string name, int age) => Console.WriteLine($"{name} ({age})");
        }

        [Fact]
        public void OneOptionalOption_OneRequiredArg()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "Cysharp" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_OneOptionalOption_OneRequiredArg>(args);
            console.Output.Should().Contain("Cysharp (17)");
        }

        [Fact]
        public void OneOptionalOption_OneRequiredArg_Option()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "Cysharp", "-age", "18" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_OneOptionalOption_OneRequiredArg>(args);
            console.Output.Should().Contain("Cysharp (18)");
        }

        [Fact]
        public void OneOptionalOption_OneRequiredArg_Help()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_OneOptionalOption_OneRequiredArg>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Options:");
        }

        public class CommandTests_OneOptionalOption_OneRequiredArg : ConsoleAppBase
        {
            public void Hello([Option(0)]string name, int age = 17) => Console.WriteLine($"{name} ({age})");
        }
    }
}
