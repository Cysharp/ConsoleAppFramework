namespace ConsoleAppFramework.GeneratorTests;

[ClassDataSource<VerifyHelper>]
public class HiddenAtttributeTest(VerifyHelper verifier)
{
    [Test]
    public async Task VerifyHiddenOptions_Lambda()
    {
        var code =
            """
            ConsoleApp.Log = x => Console.WriteLine(x);
            ConsoleApp.Run(args, (int x, [Hidden]int y) => { });
            """;

        // Verify Hidden options is not shown on command help.
        await verifier.Execute(code, args: "--help", expected:
            """
            Usage: [options...] [-h|--help] [--version]

            Options:
              --x <int>     [Required]

            """);
    }

    [Test]
    public async Task VerifyHiddenCommands_Class()
    {
        var code =
            """
            ConsoleApp.Log = x => Console.WriteLine(x);
            var builder = ConsoleApp.Create();
            builder.Add<Commands>();
            await builder.RunAsync(args);

            public class Commands
            {
                [Hidden]
                public async Task Command1() { Console.Write("command1"); }

                public async Task Command2() { Console.Write("command2"); }

                [Hidden]
                public async Task Command3(int x, [Hidden]int y) { Console.Write($"command3: x={x} y={y}"); }
            }
            """;

        // Verify hidden command is not shown on root help commands.
        await verifier.Execute(code, args: "--help", expected:
            """
            Usage: [command] [-h|--help] [--version]

            Commands:
              command2

            """);

        // Verify Hidden command help is shown when explicitly specify command name.
        await verifier.Execute(code, args: "command1 --help", expected:
            """
            Usage: command1 [-h|--help] [--version]
            
            """);

        await verifier.Execute(code, args: "command2 --help", expected:
            """
            Usage: command2 [-h|--help] [--version]

            """);

        await verifier.Execute(code, args: "command3 --help", expected:
            """
            Usage: command3 [options...] [-h|--help] [--version]

            Options:
              --x <int>     [Required]

            """);

        // Verify commands involations
        await verifier.Execute(code, args: "command1", "command1");
        await verifier.Execute(code, args: "command2", "command2");
        await verifier.Execute(code, args: "command3 --x 1 --y 2", expected: "command3: x=1 y=2");
    }

    [Test]
    public async Task VerifyHiddenCommands_LocalFunctions()
    {
        var code =
            """
                ConsoleApp.Log = x => Console.WriteLine(x);
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

        await verifier.Execute(code, args: "--help", expected:
            """
            Usage: [command] [-h|--help] [--version]

            Commands:
              command2

            """);

        // Verify commands can be invoked.
        await verifier.Execute(code, args: "command1", expected: "command1");
        await verifier.Execute(code, args: "command2", expected: "command2");
        await verifier.Execute(code, args: "command3 --x 1 --y 2", expected: "command3: x=1 y=2");
    }
}
