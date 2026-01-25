namespace ConsoleAppFramework.GeneratorTests.Bind;

[ClassDataSource<VerifyHelper>]
public class BindRecordTests(VerifyHelper verifier)
{
    [Test]
    public async Task RecordWithPrimaryConstructor()
    {
        // language=csharp
        var code = """
using System;

public record MoveOptions(bool Force, bool Recursive, bool Verbose);

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] MoveOptions options) =>
        {
            Console.Write($"Force={options.Force}, Recursive={options.Recursive}, Verbose={options.Verbose}");
        });
    }
}
""";

        await verifier.Execute(code, "--force --recursive", "Force=True, Recursive=True, Verbose=False");
    }

    [Test]
    public async Task RecordWithPrimaryConstructorDefaults()
    {
        // language=csharp
        var code = """
using System;

public record CopyOptions(bool Force = false, bool Recursive = true, bool Verbose = false);

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] CopyOptions options) =>
        {
            Console.Write($"Force={options.Force}, Recursive={options.Recursive}, Verbose={options.Verbose}");
        });
    }
}
""";

        await verifier.Execute(code, "--force", "Force=True, Recursive=True, Verbose=False");
        // With no args, all keep their defaults (including Recursive=true)
        await verifier.Execute(code, "", "Force=False, Recursive=True, Verbose=False");
    }

    [Test]
    public async Task RecordWithMixedConstructorAndProperties()
    {
        // language=csharp
        var code = """
using System;

public record Config(string Name, int Priority = 0)
{
    public bool Enabled { get; init; } = true;
    public string Description { get; init; } = "default-desc";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Name={config.Name}, Priority={config.Priority}, Enabled={config.Enabled}, Desc={config.Description}");
        });
    }
}
""";

        // Init-only properties WITHOUT 'required' preserve class defaults when not specified
        await verifier.Execute(code, "--name test --priority 5 --description hello --enabled", "Name=test, Priority=5, Enabled=True, Desc=hello");
        // Without --enabled and --description, they keep their class initializer defaults (true and "default-desc")
        await verifier.Execute(code, "--name test --priority 5", "Name=test, Priority=5, Enabled=True, Desc=default-desc");
    }
}
