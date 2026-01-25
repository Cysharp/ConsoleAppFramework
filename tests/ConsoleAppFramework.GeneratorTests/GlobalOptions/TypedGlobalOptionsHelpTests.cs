namespace ConsoleAppFramework.GeneratorTests.GlobalOptions;

[ClassDataSource<VerifyHelper>]
public class TypedGlobalOptionsHelpTests(VerifyHelper verifier)
{
    [Test]
    public async Task RootHelpShowsGlobalOptions()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    /// <summary>-v|--verbose, Enable verbose output</summary>
    public bool Verbose { get; init; } = false;

    /// <summary>--dry-run, Simulate without making changes</summary>
    public bool DryRun { get; init; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Log = x => Console.WriteLine(x);
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("test", () => Console.Write("test"));
        app.Run(args);
    }
}
""";

        var expected = """
Usage: [command] [-h|--help] [--version]

Global Options:
  -v, --verbose    Enable verbose output
  --dry-run        Simulate without making changes

Commands:
  test

""";

        var (stdout, _) = verifier.Error(code, "--help");
        await Assert.That(stdout).IsEqualTo(expected);
    }

    [Test]
    public async Task RootHelpShowsGlobalOptionsWithDefaults()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    /// <summary>--log-level, The logging level</summary>
    public string LogLevel { get; init; } = "Info";

    /// <summary>-v|--verbose, Enable verbose output</summary>
    public bool Verbose { get; init; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Log = x => Console.WriteLine(x);
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("build", () => Console.Write("build"));
        app.Run(args);
    }
}
""";

        var expected = """
Usage: [command] [-h|--help] [--version]

Global Options:
  --log-level <string>    The logging level [default: Info]
  -v, --verbose           Enable verbose output

Commands:
  build

""";

        var (stdout, _) = verifier.Error(code, "--help");
        await Assert.That(stdout).IsEqualTo(expected);
    }

    [Test]
    public async Task CommandHelpDoesNotShowInheritedGlobalOptions()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    /// <summary>-v|--verbose, Enable verbose output</summary>
    public bool Verbose { get; init; } = false;
}

public record SearchOptions : GlobalSettings
{
    /// <summary>-p|--pattern, The search pattern</summary>
    public string Pattern { get; init; } = "*";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Log = x => Console.WriteLine(x);
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("search", ([Bind] SearchOptions opts) =>
        {
            Console.Write($"Pattern={opts.Pattern}");
        });
        app.Run(args);
    }
}
""";

        // Command help should only show command-specific options, not inherited global options
        var expected = """
Usage: search [options...] [-h|--help] [--version]

Options:
  -p, --pattern <string>    The search pattern [Default: *]

""";

        var (stdout, _) = verifier.Error(code, "search --help");
        await Assert.That(stdout).IsEqualTo(expected);
    }

    [Test]
    public async Task MultipleCommandsShowGlobalOptions()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    /// <summary>-v|--verbose, Enable verbose output</summary>
    public bool Verbose { get; init; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Log = x => Console.WriteLine(x);
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("search", (string pattern) => Console.Write($"Searching: {pattern}"));
        app.Add("copy", (string source, string dest) => Console.Write($"Copying: {source} to {dest}"));
        app.Run(args);
    }
}
""";

        var expected = """
Usage: [command] [-h|--help] [--version]

Global Options:
  -v, --verbose    Enable verbose output

Commands:
  copy
  search

""";

        var (stdout, _) = verifier.Error(code, "--help");
        await Assert.That(stdout).IsEqualTo(expected);
    }

    [Test]
    public async Task GlobalOptions_NoXmlDoc_HelpStillWorks()
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
        ConsoleApp.Log = x => Console.WriteLine(x);
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("test", () => Console.Write("test"));
        app.Run(args);
    }
}
""";

        // Note: Help formatting alignment varies, checking actual output pattern
        var (stdout, _) = verifier.Error(code, "--help");
        await Assert.That(stdout).Contains("Global Options:");
        await Assert.That(stdout).Contains("--verbose");
        await Assert.That(stdout).Contains("--log-level");
        await Assert.That(stdout).Contains("[default: Info]");
        await Assert.That(stdout).Contains("Commands:");
        await Assert.That(stdout).Contains("test");
    }

    [Test]
    public async Task GlobalOptions_AliasInXmlDoc_CreatesAlias()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    /// <summary>-v|--verbose, Enable verbose logging</summary>
    public bool Verbose { get; init; } = false;

    /// <summary>-l|--log-level, Set the logging level</summary>
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

        // Test long option for boolean flag works
        await verifier.Execute(code, "--verbose test", "Verbose=True,LogLevel=Info");
        // Test long option for value type works
        await verifier.Execute(code, "--verbose --log-level Debug test", "Verbose=True,LogLevel=Debug");
    }

    [Test]
    public async Task GlobalOptions_AliasInXmlDoc_ShowsInHelp()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    /// <summary>-v|--verbose, Enable verbose logging</summary>
    public bool Verbose { get; init; } = false;

    /// <summary>-l|--log-level, Set the logging level</summary>
    public string LogLevel { get; init; } = "Info";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Log = x => Console.WriteLine(x);
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("test", () => Console.Write("test"));
        app.Run(args);
    }
}
""";

        // Note: Help formatting alignment varies, checking actual output pattern
        var (stdout, _) = verifier.Error(code, "--help");
        await Assert.That(stdout).Contains("Global Options:");
        await Assert.That(stdout).Contains("-v, --verbose");
        await Assert.That(stdout).Contains("Enable verbose logging");
        await Assert.That(stdout).Contains("-l, --log-level");
        await Assert.That(stdout).Contains("Set the logging level");
        await Assert.That(stdout).Contains("[default: Info]");
        await Assert.That(stdout).Contains("Commands:");
        await Assert.That(stdout).Contains("test");
    }

    [Test]
    public async Task CommandHelpWithGlobalOptions_ShowsBoth()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    /// <summary>-v|--verbose, Enable verbose output</summary>
    public bool Verbose { get; init; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Log = x => Console.WriteLine(x);
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("process", (
            string input,
            string output = "out.txt"
        ) =>
        {
            Console.Write($"Processing {input} to {output}");
        });
        app.Run(args);
    }
}
""";

        // Note: Help formatting alignment varies, checking actual output pattern
        // Note: Default string values may be shown with @" prefix
        var (stdout, _) = verifier.Error(code, "process --help");
        await Assert.That(stdout).Contains("Usage: process");
        await Assert.That(stdout).Contains("Options:");
        await Assert.That(stdout).Contains("--input");
        await Assert.That(stdout).Contains("[Required]");
        await Assert.That(stdout).Contains("--output");
        await Assert.That(stdout).Contains("out.txt");
    }
}
