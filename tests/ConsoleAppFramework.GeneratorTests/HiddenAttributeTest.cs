namespace ConsoleAppFramework.GeneratorTests;

public class HiddenAtttributeTest(ITestOutputHelper output)
{
    VerifyHelper verifier = new(output, "CAF");

    [Fact]
    public void VerifyHiddenOptions_Lambda()
    {
        var code =
            """
            ConsoleApp.Run(args, (int x, [Hidden]int y) => { });
            """;

        // Verify Hidden options is not shown on command help.
        verifier.Execute(code, args: "--help", expected:
            """
            Usage: [options...] [-h|--help] [--version]

            Options:
              --x <int>     (Required)

            """);
    }

    [Fact]
    public void VerifyHiddenCommands_Class()
    {
        var code =
            """
            var builder = ConsoleApp.Create();
            builder.Add<Commands>();
            await builder.RunAsync(args);

            public class Commands
            {
                [Hidden]
                public void Command1() { Console.Write("command1"); }

                public void Command2() { Console.Write("command2"); }

                [Hidden]
                public void Command3(int x, [Hidden]int y) { Console.Write($"command3: x={x} y={y}"); }
            }
            """;

        // Verify hidden command is not shown on root help commands.
        verifier.Execute(code, args: "--help", expected:
            """
            Usage: [command] [-h|--help] [--version]

            Commands:
              command2

            """);

        // Verify Hidden command help is shown when explicitly specify command name.
        verifier.Execute(code, args: "command1 --help", expected:
            """
            Usage: command1 [-h|--help] [--version]
            
            """);

        verifier.Execute(code, args: "command2 --help", expected:
            """
            Usage: command2 [-h|--help] [--version]

            """);

        verifier.Execute(code, args: "command3 --help", expected:
            """
            Usage: command3 [options...] [-h|--help] [--version]

            Options:
              --x <int>     (Required)

            """);

        // Verify commands involations
        verifier.Execute(code, args: "command1", "command1");
        verifier.Execute(code, args: "command2", "command2");
        verifier.Execute(code, args: "command3 --x 1 --y 2", expected: "command3: x=1 y=2");
    }

    [Fact]
    public void VerifyHiddenCommands_LocalFunctions()
    {
        var code =
            """
                var builder = ConsoleApp.Create();
            
                builder.Add("", () => { Console.Write("root"); });
                builder.Add("command1", Command1);
                builder.Add("command2", Command2);
                builder.Add("command3", Command3);
                builder.Run(args);

                [Hidden]
                static void Command1() { Console.Write("command1"); }

                static void Command2() { Console.Write("command2"); }

                [Hidden]
                static void Command3(int x, [Hidden]int y) { Console.Write($"command3: x={x} y={y}"); }
            """;

        verifier.Execute(code, args: "--help", expected:
            """
            Usage: [command] [-h|--help] [--version]

            Commands:
              command2

            """);

        // Verify commands can be invoked.
        verifier.Execute(code, args: "command1", expected: "command1");
        verifier.Execute(code, args: "command2", expected: "command2");
        verifier.Execute(code, args: "command3 --x 1 --y 2", expected: "command3: x=1 y=2");
    }
}
