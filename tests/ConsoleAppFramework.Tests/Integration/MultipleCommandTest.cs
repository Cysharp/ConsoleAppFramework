using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Xunit;

// ReSharper disable InconsistentNaming

namespace ConsoleAppFramework.Integration.Test
{
    public partial class MultipleCommandTest
    {
        [Fact]
        public async Task NoCommandAttribute()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { };
            (await Assert.ThrowsAsync<InvalidOperationException>(()=> Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_NoCommandAttribute>(args)))
                .Message.Should().Contain("Found more than one default command.");
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

        //[Fact]
        //public void Commands_UnknownCommand()
        //{
        //    using var console = new CaptureConsoleOutput();
        //    var args = new string[] { "unknown-command" };
        //    Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_Commands>(args);
        //    console.Output.Should().Contain("Usage:");
        //    console.Output.Should().Contain("Commands:");
        //    console.Output.Should().Contain("hello");
        //    console.Output.Should().Contain("konnichiwa");
        //}

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
        public void OptionAndArg_Option_ReverseOrdered()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "hello", "-age", "-128", "Cysharp" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_OptionAndArg>(args);
            console.Output.Should().Contain("Hello Cysharp (-128)");
        }

        [Fact]
        public void OptionAndArg_Help()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "hello", "help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_OptionAndArg>(args);
            // console.Output.Should().Contain("Hello help (18)");
        }

        [Fact]
        public void OptionAndArg_HelpAndOtherArgs()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "hello", "--help", "-age", "-128" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_OptionAndArg>(args);

            console.Output.Should().Contain("Usage: hello");
        }

        [Fact]
        public void OptionAndArg_HelpOptionLike()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "hello", "-help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_OptionAndArg>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Arguments:");

            // NOTE: Currently, ConsoleAppFramework treats the first argument as special. If the argument is '-help', it is same as '-help' option.
            //console.Output.Should().Contain("Hello -help (-128)");
        }

        [Fact]
        public void OptionAndArg_HelpOptionLikeAndOtherOptions()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "hello", "--help", "-age", "-128" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_OptionAndArg>(args);

            console.Output.Should().Contain("Usage: hello");
        }

        [Fact]
        public void CommandHelp_OptionAndArg()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "hello", "--help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_OptionAndArg>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Arguments:");
        }

        public class CommandTests_Multiple_OptionAndArg : ConsoleAppBase
        {
            [Command("hello")]
            public void Hello([Option(0)]string name, int age = 18) => Console.WriteLine($"Hello {name} ({age})");
            [Command("konnichiwa")]
            public void Konnichiwa() => Console.WriteLine("Konnichiwa");
        }

        [Fact]
        public void OptionAndArg_Option_MixedOrdered_Default()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "hello", "-age", "18", "Hello", "Cysharp" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_OptionAndArg_MixedOrdered>(args);
            console.Output.Should().Contain("Hello Cysharp (18)");
        }

        [Fact]
        public void OptionAndArg_Option_MixedOrdered_2()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "hello", "Hello", "-age", "18", "Cysharp" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_OptionAndArg_MixedOrdered>(args);
            console.Output.Should().Contain("Hello Cysharp (18)");
        }

        [Fact]
        public void OptionAndArg_Option_MixedOrdered_3()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "hello", "Hello", "Cysharp", "-age", "18" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_OptionAndArg_MixedOrdered>(args);
            console.Output.Should().Contain("Hello Cysharp (18)");
        }

        [Fact]
        public void OptionAndArg_Option_Mixed_Optional()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "greet" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_OptionAndArg_MixedOrdered>(args);
            console.Output.Should().Contain("Konnichiwa Anonymous");
        }

        public class CommandTests_Multiple_OptionAndArg_MixedOrdered : ConsoleAppBase
        {
            [Command("hello")]
            public void Hello([Option(1)]string name, int age, [Option(0)]string greeting) => Console.WriteLine($"{greeting} {name} ({age})");
            [Command("greet")]
            public void Greet([Option(0)]string greeting = "Konnichiwa", [Option(0)]string name = "Anonymous") => Console.WriteLine($"{greeting} {name}");
        }

        [Fact]
        public void OptionHelp()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "--help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_Commands>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Commands:");
            console.Output.Should().Contain("hello");
            console.Output.Should().Contain("konnichiwa");
        }

        [Fact]
        public void OptionVersion()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "--version" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Multiple_Commands>(args);
            console.Output.Should().MatchRegex(@"\d.\d.\d"); // NOTE: When running with unit test runner, it returns a version of the runner.
        }

    }
}
