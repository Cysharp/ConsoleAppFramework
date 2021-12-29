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
        public void OneRequiredOption_NoArgs()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-name", "Cysharp" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_OneRequiredOption_NoArgs>(args);
            console.Output.Should().Contain("Hello Cysharp");
        }

        [Fact]
        public void OneRequiredOption_NoArgs_OptionLikeValue()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-name", "-help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_OneRequiredOption_NoArgs>(args);
            console.Output.Should().Contain("Hello -help");
        }

        [Fact]
        public void OneRequiredOption_NoArgs_Insufficient()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_OneRequiredOption_NoArgs>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Options:");
        }

        [Fact]
        public void OneRequiredOption_NoArgs_Help()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_OneRequiredOption_NoArgs>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Options:");
        }

        public class CommandTests_Single_OneRequiredOption_NoArgs : ConsoleAppBase
        {
            public void Hello(string name) => Console.WriteLine($"Hello {name}");
        }

        [Fact]
        public void OneRequiredOneOptionalOptions_NoArgs_0()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-name", "Cysharp" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_OneRequiredOneOptionalOptions_NoArgs>(args);
            console.Output.Should().Contain("Hello Cysharp (17)");
        }

        [Fact]
        public void OneRequiredOneOptionalOptions_NoArgs_1()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-name", "Cysharp", "-age", "256" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_OneRequiredOneOptionalOptions_NoArgs>(args);
            console.Output.Should().Contain("Hello Cysharp (256)");
        }

        [Fact]
        public void OneRequiredOneOptionalOptions_NoArgs_OptionLikeValue()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-name", "-help", "-age", "256" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_OneRequiredOneOptionalOptions_NoArgs>(args);
            console.Output.Should().Contain("Hello -help (256)");
        }

        [Fact]
        public void OneRequiredOneOptionalOptions_NoArgs_Insufficient()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_OneRequiredOneOptionalOptions_NoArgs>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Options:");
        }

        [Fact]
        public void OneRequiredOneOptionalOptions_NoArgs_Help()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_OneRequiredOneOptionalOptions_NoArgs>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Options:");
        }

        public class CommandTests_Single_OneRequiredOneOptionalOptions_NoArgs : ConsoleAppBase
        {
            public void Hello(string name, int age = 17) => Console.WriteLine($"Hello {name} ({age})");
        }


        [Fact]
        public void TwoOptionalOptions_NoArgs_0()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-name", "Cysharp" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_TwoOptionalOptions_NoArgs>(args);
            console.Output.Should().Contain("Hello Cysharp (17)");
        }

        [Fact]
        public void TwoOptionalOptions_NoArgs_1()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-name", "Cysharp", "-age", "256" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_TwoOptionalOptions_NoArgs>(args);
            console.Output.Should().Contain("Hello Cysharp (256)");
        }

        [Fact]
        public void TwoOptionalOptions_NoArgs_2()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-age", "-256" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_TwoOptionalOptions_NoArgs>(args);
            console.Output.Should().Contain("Hello Anonymous (-256)");
        }

        [Fact]
        public void TwoOptionalOptions_NoArgs_Ambiguous()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-name", "-help", "-age", "256" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_TwoOptionalOptions_NoArgs>(args);
            console.Output.Should().Contain("Hello -help (256)");
            // console.GetOutputText().Should().Contain("Usage:");
            // console.GetOutputText().Should().Contain("Options:");
        }

        [Fact]
        public void TwoOptionalOptions_NoArgs_Help()
        {
            using var console = new CaptureConsoleOutput();
            var args = new[] { "-help" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_TwoOptionalOptions_NoArgs>(args);
            console.Output.Should().Contain("Usage:");
            console.Output.Should().Contain("Options:");
        }

        [Fact]
        public void TwoOptionalOptions_NoArgs_AllDefaultValue()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_TwoOptionalOptions_NoArgs>(args);
            console.Output.Should().Contain("Hello Anonymous (17)");
        }

        public class CommandTests_Single_TwoOptionalOptions_NoArgs : ConsoleAppBase
        {
            public void Hello(string name = "Anonymous", int age = 17) => Console.WriteLine($"Hello {name} ({age})");
        }

        [Fact]
        public void RequiredBoolAndOtherOption_NoArgs()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "-hello", "-name", "Cysharp" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_RequiredBoolAndOtherOption_NoArgs>(args);
            console.Output.Should().Contain("Hello Cysharp");
        }

        public class CommandTests_Single_RequiredBoolAndOtherOption_NoArgs : ConsoleAppBase
        {
            public void Hello(bool hello, string name) => Console.WriteLine($"{(hello ? "Hello" : "Konnichiwa")} {name}");
        }

        [Fact]
        public void OptionalBoolAndRequiredOtherOption_NoArgs()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "-name", "Cysharp" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_OptionalBoolAndRequiredOtherOption_NoArgs>(args);
            console.Output.Should().Contain("Konnichiwa Cysharp");
        }

        [Fact]
        public void OptionalBoolAndRequiredOtherOption_NoArgs_1()
        {
            using var console = new CaptureConsoleOutput();
            var args = new string[] { "-hello", "-name", "Cysharp" };
            Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<CommandTests_Single_OptionalBoolAndRequiredOtherOption_NoArgs>(args);
            console.Output.Should().Contain("Hello Cysharp");
        }

        public class CommandTests_Single_OptionalBoolAndRequiredOtherOption_NoArgs : ConsoleAppBase
        {
            public void Hello(string name, bool hello = false) => Console.WriteLine($"{(hello ? "Hello" : "Konnichiwa")} {name}");
        }
    }
}
