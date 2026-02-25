namespace ConsoleAppFramework.GeneratorTests.GlobalOptions;

[ClassDataSource<VerifyHelper>]
public class TypedGlobalOptionsInheritanceTests(VerifyHelper verifier)
{
    [Test]
    public async Task InheritanceBasic()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    public bool Verbose { get; init; } = false;
}

public record SearchOptions : GlobalSettings
{
    public string Pattern { get; init; } = "*";
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("search", ([Bind] SearchOptions opts) =>
        {
            Console.Write($"Verbose={opts.Verbose},Pattern={opts.Pattern}");
        });
        app.Run(args);
    }
}
""";

        await verifier.Execute(code, "--verbose search --pattern *.txt", "Verbose=True,Pattern=*.txt");
    }

    [Test]
    public async Task InheritanceGlobalOptionsFirst()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    public bool Verbose { get; init; } = false;
    public bool DryRun { get; init; } = false;
}

public record CopyOptions : GlobalSettings
{
    public string Source { get; init; } = "";
    public string Destination { get; init; } = "";
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("copy", ([Bind] CopyOptions opts) =>
        {
            Console.Write($"DryRun={opts.DryRun},Src={opts.Source},Dst={opts.Destination}");
        });
        app.Run(args);
    }
}
""";

        await verifier.Execute(code, "--dry-run copy --source /a --destination /b", "DryRun=True,Src=/a,Dst=/b");
    }

    [Test]
    public async Task InheritanceGlobalOptionsAfterCommand()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    public bool Verbose { get; init; } = false;
}

public record SearchOptions : GlobalSettings
{
    public string Pattern { get; init; } = "*";
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("search", ([Bind] SearchOptions opts) =>
        {
            Console.Write($"Verbose={opts.Verbose},Pattern={opts.Pattern}");
        });
        app.Run(args);
    }
}
""";

        // Global options can appear after command name too
        await verifier.Execute(code, "search --verbose --pattern *.log", "Verbose=True,Pattern=*.log");
    }

    [Test]
    public async Task InheritanceWithDefaults()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    public bool Verbose { get; init; } = false;
    public string LogLevel { get; init; } = "Info";
}

public record BuildOptions : GlobalSettings
{
    public string Configuration { get; init; } = "Debug";
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("build", ([Bind] BuildOptions opts) =>
        {
            Console.Write($"Verbose={opts.Verbose},LogLevel={opts.LogLevel},Config={opts.Configuration}");
        });
        app.Run(args);
    }
}
""";

        // Use defaults for global options, only set command-specific option
        await verifier.Execute(code, "build --configuration Release", "Verbose=False,LogLevel=Info,Config=Release");
    }

    [Test]
    public async Task MultipleCommandsWithInheritance()
    {
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    public bool Verbose { get; init; } = false;
}

public record SearchOptions : GlobalSettings
{
    public string Pattern { get; init; } = "*";
}

public record CopyOptions : GlobalSettings
{
    public string Source { get; init; } = "";
    public string Destination { get; init; } = "";
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();

        app.Add("search", ([Bind] SearchOptions opts) =>
        {
            Console.Write($"search:Verbose={opts.Verbose},Pattern={opts.Pattern}");
        });

        app.Add("copy", ([Bind] CopyOptions opts) =>
        {
            Console.Write($"copy:Verbose={opts.Verbose},Src={opts.Source},Dst={opts.Destination}");
        });

        app.Run(args);
    }
}
""";

        // Test search command
        await verifier.Execute(code, "--verbose search --pattern test", "search:Verbose=True,Pattern=test");
    }

    [Test]
    public async Task NonInheritingBind()
    {
        // When Bind type doesn't inherit from global options, all properties parsed as normal
        // language=csharp
        var code = """
using System;

public record GlobalSettings
{
    public bool Verbose { get; init; } = false;
}

// Note: Does NOT inherit from GlobalSettings
public record SearchOptions
{
    public string Pattern { get; init; } = "*";
    public bool CaseSensitive { get; init; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<GlobalSettings>();
        app.Add("search", ([Bind] SearchOptions opts, ConsoleAppContext ctx) =>
        {
            var globals = (GlobalSettings)ctx.GlobalOptions!;
            Console.Write($"Verbose={globals.Verbose},Pattern={opts.Pattern},CaseSensitive={opts.CaseSensitive}");
        });
        app.Run(args);
    }
}
""";

        await verifier.Execute(code, "--verbose search --pattern test --case-sensitive", "Verbose=True,Pattern=test,CaseSensitive=True");
    }

    [Test]
    public async Task FourLevelInheritance()
    {
        // language=csharp
        var code = """
using System;

public record Level1
{
    public bool Verbose { get; init; } = false;
}

public record Level2 : Level1
{
    public string Format { get; init; } = "json";
}

public record Level3 : Level2
{
    public string Output { get; init; } = "stdout";
}

public record Level4 : Level3
{
    public int Count { get; init; } = 1;
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<Level1>();
        app.Add("process", ([Bind] Level4 opts) =>
        {
            Console.Write($"Verbose={opts.Verbose},Format={opts.Format},Output={opts.Output},Count={opts.Count}");
        });
        app.Run(args);
    }
}
""";

        await verifier.Execute(code, "--verbose process --format xml --output file.txt --count 5", "Verbose=True,Format=xml,Output=file.txt,Count=5");
    }

    [Test]
    public async Task FourLevelInheritance_DefaultValues()
    {
        // language=csharp
        var code = """
using System;

public record Level1
{
    public bool Debug { get; init; } = true;
}

public record Level2 : Level1
{
    public int Priority { get; init; } = 5;
}

public record Level3 : Level2
{
    public string Mode { get; init; } = "auto";
}

public record Level4 : Level3
{
    public string Tag { get; init; } = "default";
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = ConsoleApp.Create();
        app.ConfigureGlobalOptions<Level1>();
        app.Add("run", ([Bind] Level4 opts) =>
        {
            Console.Write($"Debug={opts.Debug},Priority={opts.Priority},Mode={opts.Mode},Tag={opts.Tag}");
        });
        app.Run(args);
    }
}
""";

        // Use all default values
        await verifier.Execute(code, "run", "Debug=True,Priority=5,Mode=auto,Tag=default");
    }
}
