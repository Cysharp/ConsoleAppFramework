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
        public void CommandAliases_CommandIsNotSpecified()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_Aliased>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Commands:");
            console.Output.Should().Contain("alias-1");
            console.Output.Should().NotContain("alias-2");
        }

        [Fact]
        public void CommandAliases_Invoke_1()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "alias-1" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_Aliased>(args);
            console.Output.Should().Contain("Hello");
        }

        [Fact]
        public void CommandAliases_Invoke_2()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "alias-2" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_Aliased>(args);
            console.Output.Should().Contain("Hello");
        }

        [Fact]
        public void CommandAliases_CommandHelp_1()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "alias-1", "help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_Aliased>(args);
            console.Output.Should().Contain("Usage: ");
            console.Output.Should().MatchRegex("alias-1(?!([, ]*alias-2))");
            console.Output.Should().Contain("Aliases: alias-2");
            console.Output.Should().NotContain("alias-2, alias-2");
        }

        [Fact]
        public void CommandAliases_CommandHelp_2()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "alias-2", "help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_Aliased>(args);
            console.Output.Should().Contain("Usage: ");
            console.Output.Should().MatchRegex("alias-1(?!([, ]*alias-2))");
            console.Output.Should().Contain("Aliases: alias-2");
            console.Output.Should().NotContain("alias-2, alias-2");
        }

        public class CommandTests_Single_Aliased : ConsoleAppBase
        {
            [Command(new[] { "alias-1", "alias-2" })]
            public void Hello() => Console.WriteLine("Hello");
        }
    }
}
