namespace ConsoleAppFramework.GeneratorTests.GlobalOptions;

[ClassDataSource<VerifyHelper>]
public class TypedGlobalOptionsBasicTests(VerifyHelper verifier)
{
    [Test]
    public async Task GlobalOptionsBasic()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    public bool Verbose { get; init; } = false;
    public bool DryRun { get; init; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("test", () => Console.Write("test"));
        app.Run(args);
    }
}
""";

        await verifier.Execute(code, "test", "test");
    }

    [Test]
    public async Task GlobalOptionsWithVerbose()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    public bool Verbose { get; init; } = false;
    public bool DryRun { get; init; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("test", (ConsoleAppContext ctx) =>
        {
            var globals = (GlobalSettings)ctx.GlobalOptions!;
            Console.Write($"Verbose={globals.Verbose}");
        });
        app.Run(args);
    }
}
""";

        await verifier.Execute(code, "--verbose test", "Verbose=True");
    }

    [Test]
    public async Task GlobalOptions_NoAttributeRequired()
    {
        // language=csharp
        var code = """
using System;

// No special attribute required - just a plain record
public record GlobalSettings
{
    public bool Verbose { get; init; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("test", (ConsoleAppContext ctx) =>
        {
            var globals = (GlobalSettings)ctx.GlobalOptions!;
            Console.Write($"V={globals.Verbose}");
        });
        app.Run(args);
    }
}
""";

        // Plain types work without any special attribute
        await verifier.Execute(code, "--verbose test", "V=True");
    }
}
