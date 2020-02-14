using System;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Xunit;

// ReSharper disable InconsistentNaming

namespace ConsoleAppFramework.Integration.Test
{
    public partial class NamedSingleCommandTest
    {
        [Fact]
        public void NamedCommand_NoArgs_CommandIsNotSpecified()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_Named_NoArgs>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Commands:");
        }

        [Fact]
        public void NamedCommand_NoArgs_Invoke()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "hello" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_Named_NoArgs>(args);
            console.Output.Should().Contain("Hello");
        }

        [Fact]
        public void NamedCommand_NoArgs_CommandHelp()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "help", "hello" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_Named_NoArgs>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain(" hello");
        }

        public class CommandTests_Single_Named_NoArgs : ConsoleAppBase
        {
            [Command("hello")]
            public void Hello() => Console.WriteLine("Hello");
        }

        [Fact]
        public void NamedCommand_OneArg_CommandIsNotSpecified()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_Named_OneArg>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Commands:");
        }

        [Fact]
        public void NamedCommand_OneArg_Invoke()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "hello", "Cysharp" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_Named_OneArg>(args);
            console.Output.Should().Contain("Hello Cysharp");
        }

        [Fact]
        public void NamedCommand_OneArg_CommandHelp()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "help", "hello" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_Named_OneArg>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Arguments:");
        }

        public class CommandTests_Single_Named_OneArg : ConsoleAppBase
        {
            [Command("hello")]
            public void Hello([Option(0)]string name) => Console.WriteLine($"Hello {name}");
        }

    }
}
