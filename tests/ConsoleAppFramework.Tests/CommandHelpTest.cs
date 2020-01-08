using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using FluentAssertions.Common;
using Xunit;

namespace ConsoleAppFramework.Tests
{
    public class CommandHelpTest
    {
        private CommandHelpBuilder CreateCommandHelpBuilder() => new CommandHelpBuilder(() => "Nantoka");

        [Fact]
        public void BuildMethodListMessage()
        {
            var builder = CreateCommandHelpBuilder();
            var expected = @$"
Commands:
  CommandHelpTestListMessageBatch.Hello    
  YetAnotherHello                          
  HelloWithAliasWithDescription            Description of command

".TrimStart();

            builder.BuildMethodListMessage(new [] { typeof(CommandHelpTestListMessageBatch) }).Should().Be(expected);
        }


        [Fact]
        public void BuildUsageMessage_Types()
        {
            var builder = CreateCommandHelpBuilder();
            var expected = @"Usage: Nantoka <Command>

Commands:
  CommandHelpTestListMessageBatch.Hello    
  YetAnotherHello                          
  HelloWithAliasWithDescription            Description of command

";

            builder.BuildHelpMessage(new[] { typeof(CommandHelpTestListMessageBatch) }).Should().Be(expected);
        }

        [Fact]
        public void BuildUsageMessage_Type()
        {
            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.Complex)));
            var expected = @"Usage: Nantoka Complex2 <1st> <2nd> <3rd> [options...]";

            builder.BuildUsageMessage(def, showCommandName: true).Should().Be(expected);
        }

        [Fact]
        public void BuildUsageMessage_Single()
        {
            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.Complex)));
            var expected = @"Usage: Nantoka <1st> <2nd> <3rd> [options...]";

            builder.BuildUsageMessage(def, showCommandName: false).Should().Be(expected);
        }

        [Fact]
        public void BuildUsageMessage_Single_IndexedOptionsOnly()
        {
            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.ComplexIndexedOnly)));
            var expected = @"Usage: Nantoka <1st> <2nd> <3rd>";

            builder.BuildUsageMessage(def, showCommandName: false).Should().Be(expected);
        }


        [Fact]
        public void CreateCommandHelp_Single_NoDescription()
        {
            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.ComplexIndexedOnlyNoDescription)));
            var expected = @"
Usage: Nantoka <1st> <2nd> <3rd>

Arguments:
  [0] <Boolean>    1st
  [1] <String>     2nd
  [2] <Int32>      3rd

".TrimStart();

            builder.BuildHelpMessage(def, showCommandName: false).Should().Be(expected);
        }

        [Fact]
        public void CreateCommandHelp_Single_IndexedOptionsOnly()
        {
            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.ComplexIndexedOnly)));
            var expected = @"
Usage: Nantoka <1st> <2nd> <3rd>

Description of complex command2

Arguments:
  [0] <Boolean>    1st
  [1] <String>     2nd
  [2] <Int32>      3rd

".TrimStart();

            builder.BuildHelpMessage(def, showCommandName: false).Should().Be(expected);
        }

        [Fact]
        public void CreateCommandHelp_Single()
        {
            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.Complex)));
            var expected = @"
Usage: Nantoka <1st> <2nd> <3rd> [options...]

Description of complex command

Arguments:
  [0] <Boolean>    1st
  [1] <String>     2nd
  [2] <Int32>      3rd

Options:
  -anonArg0 <Int32>                  (Required)
  -optA, -shortNameArg0 <String>    Option has short name (Required)

".TrimStart();

            builder.BuildHelpMessage(def, showCommandName: false).Should().Be(expected);
        }

        [Fact]
        public void CreateCommandHelp_RequiredOrNotRequired()
        {
            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.OptionRequiredAndNotRequired)));
            var expected = @"
Options:
  -foo <String>    desc1 (Required)
  -bar <Int32>     desc2 (Default: 999)

".TrimStart();

            builder.BuildOptionsMessage(def).Should().Be(expected);
        }

        [Fact]
        public void CreateCommandHelpDefinition()
        {
            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.Hello)));
            def.Command.Should().Be("CommandHelpTestBatch.Hello");
            def.CommandAliases.Should().BeEmpty();
            def.Options.Should().BeEmpty();
            def.Description.Should().BeEmpty();
        }

        [Fact]
        public void CreateCommandHelpDefinition_Alias()
        {
            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.HelloWithAlias)));
            def.Command.Should().Be("CommandHelpTestBatch.HelloWithAlias");
            def.CommandAliases.Should().Contain("YetAnotherHello");
            def.Options.Should().BeEmpty();
            def.Description.Should().BeEmpty();
        }

        [Fact]
        public void CreateCommandHelpDefinition_Alias_Description()
        {
            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.HelloWithAliasWithDescription)));
            def.Command.Should().Be("CommandHelpTestBatch.HelloWithAliasWithDescription");
            def.CommandAliases.Should().Contain("HelloWithAliasWithDescription");
            def.Options.Should().BeEmpty();
            def.Description.Should().Be("Description of command");
        }

        [Fact]
        public void CreateCommandHelpDefinition_Aliases()
        {
            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.HelloWithAliases)));
            def.Command.Should().Be("CommandHelpTestBatch.HelloWithAliases");
            def.CommandAliases.Should().Contain(new [] { "HokanoHello", "YetAnotherHello2" });
            def.Options.Should().BeEmpty();
            def.Description.Should().BeEmpty();
        }

        [Fact]
        public void CreateCommandHelpDefinition_Options_1()
        {
            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.OptionalParameters)));
            def.Command.Should().Be("CommandHelpTestBatch.OptionalParameters");
            def.CommandAliases.Should().BeEmpty();
            def.Options.Should().NotBeEmpty();
            def.Options[0].Options.Should().Equal(new [] { "-x", "-xxx" });
            def.Options[0].Description.Should().BeEmpty();
            def.Options[0].ValueTypeName.Should().Be("Int32");
            def.Options[0].DefaultValue.Should().BeNull();
            def.Options[0].Index.Should().BeNull();
            def.Options[1].Options.Should().Equal(new[] { "-y", "-yyy" });
            def.Options[1].Description.Should().Be("Option y");
            def.Options[1].ValueTypeName.Should().Be("Int32");
            def.Options[1].DefaultValue.Should().BeNull();
            def.Options[1].Index.Should().BeNull();
            def.Description.Should().BeEmpty();
        }

        [Fact]
        public void CreateCommandHelpDefinition_Options_SameShortName()
        {
            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.OptionalParametersSameShortName)));
            def.Command.Should().Be("CommandHelpTestBatch.OptionalParametersSameShortName");
            def.CommandAliases.Should().BeEmpty();
            def.Options.Should().NotBeEmpty();
            def.Options[0].Options.Should().Equal(new [] { "-xxx" });
            def.Options[0].Description.Should().BeEmpty();
            def.Options[0].ValueTypeName.Should().Be("Int32");
            def.Options[0].DefaultValue.Should().BeNull();
            def.Options[0].Index.Should().BeNull();
            def.Options[1].Options.Should().Equal(new[] { "-yyy" });
            def.Options[1].Description.Should().Be("Option y");
            def.Options[1].ValueTypeName.Should().Be("Int32");
            def.Options[1].DefaultValue.Should().BeNull();
            def.Options[1].Index.Should().BeNull();
            def.Description.Should().BeEmpty();
        }

        [Fact]
        public void CreateCommandHelpDefinition_Options_DefaultValue()
        {
            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.OptionDefaultValue)));
            def.Command.Should().Be("CommandHelpTestBatch.OptionDefaultValue");
            def.CommandAliases.Should().BeEmpty();
            def.Options.Should().NotBeEmpty();
            def.Options[0].Options.Should().Equal(new[] { "-nano" });
            def.Options[0].Description.Should().BeEmpty();
            def.Options[0].ValueTypeName.Should().Be("Int32");
            def.Options[0].DefaultValue.Should().Be("999");
            def.Options[0].Index.Should().BeNull();
            def.Description.Should().BeEmpty();
        }

        [Fact]
        public void CreateCommandHelpDefinition_Options_Index()
        {
            var builder = CreateCommandHelpBuilder();
            var def = builder.CreateCommandHelpDefinition(typeof(CommandHelpTestBatch).GetMethod(nameof(CommandHelpTestBatch.OptionIndex)));
            def.Command.Should().Be("CommandHelpTestBatch.OptionIndex");
            def.CommandAliases.Should().BeEmpty();
            def.Options[0].Index.Should().Be(0);
            def.Options[0].Options.Should().Contain("[0]");
            def.Options[0].Description.Should().Be("1st");
            def.Options[0].ValueTypeName.Should().Be("Boolean");
            def.Options[1].Index.Should().Be(1);
            def.Options[1].Options.Should().Contain("[1]");
            def.Options[1].Description.Should().Be("2nd");
            def.Options[1].ValueTypeName.Should().Be("String");
            def.Options[2].Index.Should().Be(2);
            def.Options[2].Options.Should().Contain("[2]");
            def.Options[2].Description.Should().Be("3rd");
            def.Options[2].ValueTypeName.Should().Be("Int32");
            def.Description.Should().BeEmpty();
        }
    }

    public class CommandHelpTestListMessageBatch : BatchBase
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

    public class CommandHelpTestBatch : BatchBase
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

        [Command(new [] { "YetAnotherHello2", "HokanoHello" })]
        public void HelloWithAliases()
        {
        }

        public void OptionalParameters([Option("x")]int xxx, [Option("y", "Option y")]int yyy)
        {
        }

        public void OptionalParametersSameShortName([Option("xxx")]int xxx, [Option("yyy", "Option y")]int yyy)
        {
        }

        public void OptionDefaultValue(int nano = 999)
        {
        }

        public void OptionRequiredAndNotRequired([Option("foo", "desc1")]string foo, [Option("bar", "desc2")]int bar = 999)
        {
        }

        public void OptionIndex([Option(2, "3rd")]int arg0, [Option(1, "2nd")]string arg1, [Option(0, "1st")]bool arg2)
        {
        }

        [Command(new []{ "Complex2", "cpx" }, "Description of complex command")]
        public void Complex(
            int anonArg0,
            [Option("optA", "Option has short name")]
            string shortNameArg0,
            [Option(2, "3rd")]int arg0,
            [Option(1, "2nd")]string arg1, 
            [Option(0, "1st")]bool arg2
        )
        {
        }

        [Command("ComplexIndexedOnly", "Description of complex command2")]
        public void ComplexIndexedOnly(
            [Option(2, "3rd")]int arg0,
            [Option(1, "2nd")]string arg1,
            [Option(0, "1st")]bool arg2
        )
        {
        }

        public void ComplexIndexedOnlyNoDescription(
            [Option(2, "3rd")]int arg0,
            [Option(1, "2nd")]string arg1,
            [Option(0, "1st")]bool arg2
        )
        {
        }
    }
}
