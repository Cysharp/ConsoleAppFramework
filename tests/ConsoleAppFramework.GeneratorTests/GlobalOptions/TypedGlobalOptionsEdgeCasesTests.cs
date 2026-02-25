namespace ConsoleAppFramework.GeneratorTests.GlobalOptions;

[ClassDataSource<VerifyHelper>]
public class TypedGlobalOptionsEdgeCasesTests(VerifyHelper verifier)
{
    [Test]
    public async Task GlobalOptions_EmptyClass_NoErrors()
    {
        // language=csharp
        var code = """
using System;

public record EmptyGlobalOptions
{
    // Empty class - but still valid
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<EmptyGlobalOptions>();
        app.Add("test", () => Console.Write("test executed"));
        app.Run(args);
    }
}
""";

        await verifier.Execute(code, "test", "test executed");
    }

    [Test]
    public async Task GlobalOptions_OnlyInheritedProperties()
    {
        // language=csharp
        var code = """
using System;

public record BaseOptions
{
    public bool Verbose { get; init; } = false;
    public string LogLevel { get; init; } = "Info";
}

public record DerivedGlobalOptions : BaseOptions
{
    // No new properties, only inherited ones
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<DerivedGlobalOptions>();
        app.Add("test", (ConsoleAppContext ctx) =>
        {
            var opts = (DerivedGlobalOptions)ctx.GlobalOptions!;
            Console.Write($"Verbose={opts.Verbose},LogLevel={opts.LogLevel}");
        });
        app.Run(args);
    }
}
""";

        await verifier.Execute(code, "--verbose --log-level Debug test", "Verbose=True,LogLevel=Debug");
    }

    // Note: Required properties are NOT supported for ConfigureGlobalOptions<T>() because
    // it has a new() constraint that conflicts with required members.
    // Use non-required properties with validation instead.

    [Test]
    public async Task GlobalOptions_UnknownOption_PassesThrough()
    {
        // language=csharp
        var code = """
using System;

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
        app.Add("test", (string unknownOption, ConsoleAppContext ctx) =>
        {
            var opts = (GlobalSettings)ctx.GlobalOptions!;
            Console.Write($"Verbose={opts.Verbose},Unknown={unknownOption}");
        });
        app.Run(args);
    }
}
""";

        // Unknown global option should pass through to command
        await verifier.Execute(code, "--verbose test --unknown-option passedthrough", "Verbose=True,Unknown=passedthrough");
    }

    [Test]
    public async Task GlobalOptions_CaseSensitivity()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    public bool Verbose { get; init; } = false;
    public string LogLevel { get; init; } = "Info";
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("test", (ConsoleAppContext ctx) =>
        {
            var opts = (GlobalSettings)ctx.GlobalOptions!;
            Console.Write($"Verbose={opts.Verbose},LogLevel={opts.LogLevel}");
        });
        app.Run(args);
    }
}
""";

        // Options are case-insensitive by default
        await verifier.Execute(code, "--VERBOSE --LOG-LEVEL Debug test", "Verbose=True,LogLevel=Debug");
    }

    [Test]
    public async Task GlobalOptions_InheritedAndLocalProperties()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    public bool Verbose { get; init; } = false;
}

public record CommandOptions : GlobalSettings
{
    public string Output { get; init; } = "stdout";
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("process", ([Bind] CommandOptions opts) =>
        {
            Console.Write($"Verbose={opts.Verbose},Output={opts.Output}");
        });
        app.Run(args);
    }
}
""";

        // Global options set before command, command-specific after
        await verifier.Execute(code, "--verbose process --output file.txt", "Verbose=True,Output=file.txt");
    }

    [Test]
    public async Task GlobalOptions_MultipleCommands_SharedOptions()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    public bool Verbose { get; init; } = false;
    public string Format { get; init; } = "json";
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();

        app.Add("build", (ConsoleAppContext ctx) =>
        {
            var opts = (GlobalSettings)ctx.GlobalOptions!;
            Console.Write($"build:Verbose={opts.Verbose},Format={opts.Format}");
        });

        app.Add("test", (ConsoleAppContext ctx) =>
        {
            var opts = (GlobalSettings)ctx.GlobalOptions!;
            Console.Write($"test:Verbose={opts.Verbose},Format={opts.Format}");
        });

        app.Run(args);
    }
}
""";

        await verifier.Execute(code, "--verbose --format xml build", "build:Verbose=True,Format=xml");
        await verifier.Execute(code, "--format yaml test", "test:Verbose=False,Format=yaml");
    }

    [Test]
    public async Task GlobalOptions_WithDefaults_NotProvided()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    public bool Verbose { get; init; } = false;
    public string LogLevel { get; init; } = "Info";
    public int MaxRetries { get; init; } = 3;
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("test", (ConsoleAppContext ctx) =>
        {
            var opts = (GlobalSettings)ctx.GlobalOptions!;
            Console.Write($"Verbose={opts.Verbose},LogLevel={opts.LogLevel},MaxRetries={opts.MaxRetries}");
        });
        app.Run(args);
    }
}
""";

        // All options use defaults when not provided
        await verifier.Execute(code, "test", "Verbose=False,LogLevel=Info,MaxRetries=3");
    }

    // Note: Boolean with explicit true/false values (--verbose false) is not supported
    // for GlobalOptions. Boolean options are treated as flags where presence means true.
}
