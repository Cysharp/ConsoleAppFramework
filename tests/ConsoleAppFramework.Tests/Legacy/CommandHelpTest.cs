using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using FluentAssertions.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConsoleAppFramework.Tests
{
    public class CommandHelpTest
    {
        private CommandHelpBuilder CreateCommandHelpBuilder() => new CommandHelpBuilder(() => "Nantoka", null, null, new ConsoleAppOptions() { NameConverter = x => x.ToLower() });
        private CommandHelpBuilder CreateCommandHelpBuilder2() => new CommandHelpBuilder(() => "Nantoka", null, null, new ConsoleAppOptions() { NameConverter = x => x.ToLower() });
        private CommandHelpBuilder CreateCommandHelpBuilder3() => new CommandHelpBuilder(() => "Nantoka", null, null, new ConsoleAppOptions() { NameConverter = x => x.ToLower() });

        [Fact]
        public void BuildMethodListMessage()
        {
            var builder = CreateCommandHelpBuilder();
            var expected = @$"
Commands:
  list-message-batch hello                            
  list-message-batch YetAnotherHello                  
  list-message-batch HelloWithAliasWithDescription    Description of command
".TrimStart();

            var app = ConsoleApp.CreateBuilder(new string[0]).Build();
            app.AddSubCommands<CommandHelpTestListMessageBatch>();
            var descs = app.Host.Services.GetRequiredService<ConsoleAppOptions>().CommandDescriptors.GetSubCommands("list-message-batch");

            var msg = builder.BuildMethodListMessage(descs, false, out _);
            msg.Should().Be(expected);
        }

        [Fact]
        public void BuildUsageMessage_Types()
        {
            var builder = CreateCommandHelpBuilder();
            var expected = @"Usage: Nantoka <Command>

Commands:
  list-message-batch hello                            
  list-message-batch HelloWithAliasWithDescription    Description of command
  list-message-batch YetAnotherHello                  
";

            var app = ConsoleApp.CreateBuilder(new string[0]).Build();
            app.AddSubCommands<CommandHelpTestListMessageBatch>();
            var descs = app.Host.Services.GetRequiredService<ConsoleAppOptions>().CommandDescriptors.GetSubCommands("list-message-batch");

            builder.BuildHelpMessage(null, descs, false).Should().Be(expected);
        }

        [Fact]
        public void BuildUsageMessage_Type()
        {
            var app = ConsoleApp.CreateBuilder(new string[0]).Build();
            app.AddSubCommand("commandhelptestbatch", "Complex2", new CommandHelpTestBatch().Complex);
            app.Host.Services.GetRequiredService<ConsoleAppOptions>().CommandDescriptors.TryGetDescriptor(new[] { "commandhelptestbatch", "Complex2" }, out var desc, out _);

            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(desc, false);
            var expected = @"Usage: Nantoka commandhelptestbatch Complex2 <1st> <2nd> <3rd> [options...]";

            builder.BuildUsageMessage(def, showCommandName: true, fromMultiCommand: true).Should().Be(expected);
        }

        [Fact]
        public void BuildUsageMessage_Single()
        {
            var app = ConsoleApp.CreateBuilder(new string[0]).Build();
            app.AddSubCommand("commandhelptestbatch", "Complex2", new CommandHelpTestBatch().Complex);
            app.Host.Services.GetRequiredService<ConsoleAppOptions>().CommandDescriptors.TryGetDescriptor(new[] { "commandhelptestbatch", "Complex2" }, out var desc, out _);

            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(desc, true);
            var expected = @"Usage: Nantoka <1st> <2nd> <3rd> [options...]";

            builder.BuildUsageMessage(def, showCommandName: false, fromMultiCommand: false).Should().Be(expected);
        }

        //[Fact]
        //public void BuildUsageMessage_Single_IndexedOptionsOnly()
        //{
        //    var builder = CreateCommandHelpBuilder();
        //    var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.ComplexIndexedOnly)));
        //    var expected = @"Usage: Nantoka <1st> <2nd> <3rd>";

        //    builder.BuildUsageMessage(def, showCommandName: false, fromMultiCommand: false).Should().Be(expected);
        //}


        //        [Fact]
        //        public void CreateCommandHelp_Single_NoDescription()
        //        {
        //            var builder = CreateCommandHelpBuilder();
        //            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.ComplexIndexedOnlyNoDescription)));
        //            var expected = @"
        //Usage: Nantoka <1st> <2nd> <3rd>

        //Arguments:
        //  [0] <Boolean>    1st
        //  [1] <String>     2nd
        //  [2] <Int32>      3rd

        //".TrimStart();

        //            builder.BuildHelpMessage(def, showCommandName: false, fromMultiCommand: false).Should().Be(expected);
        //        }

        //        [Fact]
        //        public void CreateCommandHelp_Single_IndexedOptionsOnly()
        //        {
        //            var builder = CreateCommandHelpBuilder();
        //            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.ComplexIndexedOnly)));
        //            var expected = @"
        //Usage: Nantoka <1st> <2nd> <3rd>

        //Description of complex command2

        //Arguments:
        //  [0] <Boolean>    1st
        //  [1] <String>     2nd
        //  [2] <Int32>      3rd

        //".TrimStart();

        //            builder.BuildHelpMessage(def, showCommandName: false, fromMultiCommand: false).Should().Be(expected);
        //        }

        //        [Fact]
        //        public void CreateCommandHelp_Single()
        //        {
        //            {
        //                var builder = CreateCommandHelpBuilder();
        //                var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.Complex)));
        //                var expected = @"
        //Usage: Nantoka <1st> <2nd> <3rd> [options...]

        //Description of complex command

        //Arguments:
        //  [0] <Boolean>    1st
        //  [1] <String>     2nd
        //  [2] <Int32>      3rd

        //Options:
        //  -anonArg0 <Int32>                  (Required)
        //  -optA, -shortNameArg0 <String>    Option has short name (Required)

        //".TrimStart();

        //                builder.BuildHelpMessage(def, showCommandName: false, fromMultiCommand: false).Should().Be(expected);
        //            }
        //            {
        //                var builder = CreateCommandHelpBuilder2();
        //                var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.Complex)));
        //                var expected = @"
        //Usage: Nantoka <1st> <2nd> <3rd> [options...]

        //Description of complex command

        //Arguments:
        //  [0] <Boolean>    1st
        //  [1] <String>     2nd
        //  [2] <Int32>      3rd

        //Options:
        //  --anonArg0 <Int32>                  (Required)
        //  -optA, --shortNameArg0 <String>    Option has short name (Required)

        //".TrimStart();

        //                builder.BuildHelpMessage(def, showCommandName: false, fromMultiCommand: false).Should().Be(expected);
        //            }
        //        }

        //        [Fact]
        //        public void CreateCommandHelp_RequiredOrNotRequired()
        //        {
        //            var builder = CreateCommandHelpBuilder();
        //            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.OptionRequiredAndNotRequired)));
        //            var expected = @"
        //Options:
        //  -foo <String>    desc1 (Required)
        //  -bar <Int32>     desc2 (Default: 999)

        //".TrimStart();

        //            builder.BuildOptionsMessage(def).Should().Be(expected);
        //        }

        //        [Fact]
        //        public void CreateCommandHelp_BooleanWithoutDefault_ShownWithoutValue()
        //        {
        //            var builder = CreateCommandHelpBuilder();
        //            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.OptionBooleanSwitchWithoutDefault)));
        //            var expected = @"
        //Options:
        //  -f, -flag    desc (Optional)

        //".TrimStart();

        //            builder.BuildOptionsMessage(def).Should().Be(expected);
        //        }

        //        [Fact]
        //        public void CreateCommandHelp_BooleanWithTrueDefault_ShownWithValue()
        //        {
        //            var builder = CreateCommandHelpBuilder();
        //            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.OptionBooleanSwitchWithTrueDefault)));
        //            var expected = @"
        //Options:
        //  -f, -flag <Boolean>    desc (Default: True)

        //".TrimStart();

        //            builder.BuildOptionsMessage(def).Should().Be(expected);
        //        }

        //        [Fact]
        //        public void CreateCommandHelp_BooleanWithTrueDefault_ShownWithoutValue()
        //        {
        //            var builder = CreateCommandHelpBuilder();
        //            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.OptionBooleanSwitchWithFalseDefault)));
        //            var expected = @"
        //Options:
        //  -f, -flag    desc (Optional)

        //".TrimStart();

        //            builder.BuildOptionsMessage(def).Should().Be(expected);
        //        }
    }

    [Command("list-message-batch")]
    public class CommandHelpTestListMessageBatch : ConsoleAppBase
    {
        public void Hello()
        {
        }

        [Command("YetAnotherHello")]
        public void HelloWithAlias()
        {
        }

        [Command("HelloWithAliasWithDescription", "Description of command")]
        public void HelloWithAliasWithDescription()
        {
        }
    }

    public class CommandHelpTestBatch : ConsoleAppBase
    {
        public void Hello()
        {
        }

        [Command("YetAnotherHello")]
        public void HelloWithAlias()
        {
        }

        [Command("HelloWithAliasWithDescription", "Description of command")]
        public void HelloWithAliasWithDescription()
        {
        }

        [Command(new[] { "YetAnotherHello2", "HokanoHello" })]
        public void HelloWithAliases()
        {
        }

        public void OptionalParameters([Option("x")] int xxx, [Option("y", "Option y")] int yyy)
        {
        }

        public void OptionalParametersSameShortName([Option("xxx")] int xxx, [Option("yyy", "Option y")] int yyy)
        {
        }

        public void OptionDefaultValue(int nano = 999)
        {
        }

        public void OptionRequiredAndNotRequired([Option("foo", "desc1")] string foo, [Option("bar", "desc2")] int bar = 999)
        {
        }

        public void OptionIndex([Option(2, "3rd")] int arg0, [Option(1, "2nd")] string arg1, [Option(0, "1st")] bool arg2)
        {
        }

        public void OptionBooleanSwitchWithoutDefault([Option("f", "desc")] bool flag)
        {
        }

        public void OptionBooleanSwitchWithTrueDefault([Option("f", "desc")] bool flag = true)
        {
        }

        public void OptionBooleanSwitchWithFalseDefault([Option("f", "desc")] bool flag = false)
        {
        }

        [Command(new[] { "Complex2", "cpx" }, "Description of complex command")]
        public void Complex(
            int anonArg0,
            [Option("optA", "Option has short name")]
            string shortNameArg0,
            [Option(2, "3rd")] int arg0,
            [Option(1, "2nd")] string arg1,
            [Option(0, "1st")] bool arg2
        )
        {
        }

        [Command("ComplexIndexedOnly", "Description of complex command2")]
        public void ComplexIndexedOnly(
            [Option(2, "3rd")] int arg0,
            [Option(1, "2nd")] string arg1,
            [Option(0, "1st")] bool arg2
        )
        {
        }

        public void ComplexIndexedOnlyNoDescription(
            [Option(2, "3rd")] int arg0,
            [Option(1, "2nd")] string arg1,
            [Option(0, "1st")] bool arg2
        )
        {
        }
    }
}
