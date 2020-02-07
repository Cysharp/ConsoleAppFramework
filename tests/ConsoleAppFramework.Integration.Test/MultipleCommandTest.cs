using System;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Xunit;

// ReSharper disable InconsistentNaming

namespace ConsoleAppFramework.Integration.Test
{
    public partial class MultipleCommandTest
    {
        [Fact]
        public void NoCommandAttribute()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_NoCommandAttribute>(args);
            console.Output.Should().Contain("Found more than one public methods(without command).");
        }

        public class CommandTests_Multiple_NoCommandAttribute : ConsoleAppBase
        {
            public void Hello() => Console.WriteLine("Hello");
            public void Konnichiwa() => Console.WriteLine("Konnichiwa");
        }

        [Fact]
        public void Commands()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_Commands>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Commands:");
            console.Output.Should().Contain("hello");
            console.Output.Should().Contain("konnichiwa");
        }

        [Fact]
        public void Commands_UnknownCommand()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "unknown-command" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_Commands>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Commands:");
            console.Output.Should().Contain("hello");
            console.Output.Should().Contain("konnichiwa");
        }

        [Fact]
        public void Commands_UnknownCommand_Help()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "help", "-foo", "-bar" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_Commands>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Commands:");
            console.Output.Should().Contain("hello");
            console.Output.Should().Contain("konnichiwa");
        }

        public class CommandTests_Multiple_Commands : ConsoleAppBase
        {
            [Command("hello")]
            public void Hello() => Console.WriteLine("Hello");
            [Command("konnichiwa")]
            public void Konnichiwa() => Console.WriteLine("Konnichiwa");
        }

        [Fact]
        public void OptionAndArg()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "hello", "Cysharp" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_OptionAndArg>(args);
            console.Output.Should().Contain("Hello Cysharp (18)");
        }

        [Fact]
        public void OptionAndArg_Option()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "hello", "Cysharp", "-age", "-128" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_OptionAndArg>(args);
            console.Output.Should().Contain("Hello Cysharp (-128)");
        }

        [Fact]
        public void OptionAndArg_HelpOptionLike()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "hello", "-help", "-age", "-128" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_OptionAndArg>(args);
            console.Output.Should().Contain("Hello -help (-128)");
        }

        public class CommandTests_Multiple_OptionAndArg : ConsoleAppBase
        {
            [Command("hello")]
            public void Hello([Option(0)]string name, int age = 18) => Console.WriteLine($"Hello {name} ({age})");
            [Command("konnichiwa")]
            public void Konnichiwa() => Console.WriteLine("Konnichiwa");
        }
    }
}
