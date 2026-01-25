namespace ConsoleAppFramework.GeneratorTests.GlobalOptions;

[ClassDataSource<VerifyHelper>]
public class TypedGlobalOptionsDiagnosticsTests(VerifyHelper verifier)
{
    [Test]
    public async Task GlobalOptionsWithArgumentAttribute_EmitsDiagnostic()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    [Argument]
    public string Path { get; init; } = "";

    public bool Verbose { get; init; } = false;
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

        // Verify that diagnostic CAF026 is generated
        await verifier.Verify(26, code, "GlobalSettings", "Path");
    }

    [Test]
    public async Task GlobalOptionsWithArgumentComment_EmitsDiagnostic()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    /// <summary>argument, The input file path</summary>
    public string InputFile { get; init; } = "";

    public bool Verbose { get; init; } = false;
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

        // Verify that diagnostic CAF026 is generated for "argument," comment
        await verifier.Verify(26, code, "GlobalSettings", "InputFile");
    }

    [Test]
    public async Task GlobalOptionsWithArgumentCommentCaseInsensitive_EmitsDiagnostic()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    /// ARGUMENT, The output path
    public string OutputPath { get; init; } = "";

    public bool Verbose { get; init; } = false;
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

        // Verify that diagnostic CAF026 is generated for case insensitive "ARGUMENT,"
        await verifier.Verify(26, code, "GlobalSettings", "OutputPath");
    }

    [Test]
    public async Task GlobalOptionsWithConstructorArgumentParam_EmitsDiagnostic()
    {
        // language=csharp
        var code = """
using System;

/// <summary>Global settings</summary>
/// <param name="inputFile">argument, The input file</param>
/// <param name="verbose">Enable verbose</param>
public record GlobalSettings(string inputFile, bool verbose = false);

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

        // Verify that diagnostic CAF026 is generated for constructor param with "argument," comment
        await verifier.Verify(26, code, "GlobalSettings", "inputFile");
    }

    [Test]
    public async Task GlobalOptionsWithoutArguments_NoDiagnostic()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    /// <summary>-v|--verbose, Enable verbose output</summary>
    public bool Verbose { get; init; } = false;

    /// <summary>--log-level, Set the logging level</summary>
    public string LogLevel { get; init; } = "Info";
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

        // No diagnostic should be emitted
        await verifier.Execute(code, "test", "test");
    }
}
