namespace ConsoleAppFramework.GeneratorTests.Bind;

[ClassDataSource<VerifyHelper>]
public class BindArgumentTests(VerifyHelper verifier)
{
    [Test]
    public async Task ArgumentsOnConstructorParameters()
    {
        // language=csharp
        var code = """
using System;

public record MoveArgs([Argument] string Source, [Argument] string Target, bool Force = false);

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] MoveArgs moveArgs) =>
        {
            Console.Write($"Source={moveArgs.Source}, Target={moveArgs.Target}, Force={moveArgs.Force}");
        });
    }
}
""";

        await verifier.Execute(code, "/src /dest --force", "Source=/src, Target=/dest, Force=True");
        await verifier.Execute(code, "/src /dest", "Source=/src, Target=/dest, Force=False");
    }

    [Test]
    public async Task ArgumentsOnProperties()
    {
        // language=csharp
        var code = """
using System;

public class CopyArgs
{
    [Argument] public string Source { get; set; } = "";
    [Argument] public string Destination { get; set; } = "";
    public bool Recursive { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] CopyArgs copyArgs) =>
        {
            Console.Write($"Source={copyArgs.Source}, Dest={copyArgs.Destination}, Recursive={copyArgs.Recursive}");
        });
    }
}
""";

        await verifier.Execute(code, "/src /dest --recursive", "Source=/src, Dest=/dest, Recursive=True");
        await verifier.Execute(code, "/src /dest", "Source=/src, Dest=/dest, Recursive=False");
    }

    [Test]
    public async Task ArgumentWithDefault_IsOptional()
    {
        // language=csharp
        var code = """
using System;

public record CopyOptions([Argument] string Source, [Argument] string Destination = ".", bool Recursive = false);

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] CopyOptions opts) =>
        {
            Console.Write($"Source={opts.Source}, Dest={opts.Destination}, Recursive={opts.Recursive}");
        });
    }
}
""";

        // Both arguments provided
        await verifier.Execute(code, "/src /dest", "Source=/src, Dest=/dest, Recursive=False");
        // Only required argument - Destination uses default "."
        await verifier.Execute(code, "/src", "Source=/src, Dest=., Recursive=False");
        // With option
        await verifier.Execute(code, "/src --recursive", "Source=/src, Dest=., Recursive=True");
    }

    [Test]
    public async Task ArgumentWithoutDefault_IsRequired()
    {
        // language=csharp
        var code = """
using System;

public record MoveOptions([Argument] string Source, [Argument] string Destination);

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] MoveOptions opts) =>
        {
            Console.Write($"Source={opts.Source}, Dest={opts.Destination}");
        });
    }
}
""";

        // Both required arguments provided
        await verifier.Execute(code, "/src /dest", "Source=/src, Dest=/dest");

        // Missing second required argument should fail
        var (_, exitCode1) = verifier.Error(code, "/src");
        await Assert.That(exitCode1).IsNotEqualTo(0);

        // Missing both should fail
        var (_, exitCode2) = verifier.Error(code, "");
        await Assert.That(exitCode2).IsNotEqualTo(0);
    }

    [Test]
    public async Task ArgumentOnPropertyWithDefault_IsOptional()
    {
        // language=csharp
        var code = """
using System;

public class SearchArgs
{
    [Argument] public string Pattern { get; set; } = "*";
    [Argument] public string Path { get; set; } = ".";
    public bool Recursive { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] SearchArgs searchArgs) =>
        {
            Console.Write($"Pattern={searchArgs.Pattern}, Path={searchArgs.Path}, Recursive={searchArgs.Recursive}");
        });
    }
}
""";

        // Both arguments provided
        await verifier.Execute(code, "*.txt /home", "Pattern=*.txt, Path=/home, Recursive=False");
        // Only first argument - Path uses default "."
        await verifier.Execute(code, "*.txt", "Pattern=*.txt, Path=., Recursive=False");
        // No arguments - both use defaults
        await verifier.Execute(code, "", "Pattern=*, Path=., Recursive=False");
        // No arguments but with option
        await verifier.Execute(code, "--recursive", "Pattern=*, Path=., Recursive=True");
        // First argument with option
        await verifier.Execute(code, "*.log --recursive", "Pattern=*.log, Path=., Recursive=True");
    }

    [Test]
    public async Task ArgumentCommentOnProperty()
    {
        // language=csharp
        var code = """
using System;

public class CopyArgs
{
    /// <summary>argument, The source file</summary>
    public string Source { get; set; } = "";

    /// <summary>argument, The destination file</summary>
    public string Destination { get; set; } = "";

    public bool Recursive { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] CopyArgs copyArgs) =>
        {
            Console.Write($"Source={copyArgs.Source}, Dest={copyArgs.Destination}, Recursive={copyArgs.Recursive}");
        });
    }
}
""";

        // Using "argument," in XML doc comment marks property as positional argument
        await verifier.Execute(code, "/src /dest --recursive", "Source=/src, Dest=/dest, Recursive=True");
        await verifier.Execute(code, "/src /dest", "Source=/src, Dest=/dest, Recursive=False");
    }

    [Test]
    public async Task ArgumentCommentOnConstructorParam()
    {
        // language=csharp
        var code = """
using System;

/// <summary>Move arguments</summary>
/// <param name="source">argument, The source path</param>
/// <param name="target">argument, The target path</param>
public record MoveArgs(string source, string target, bool force = false);

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] MoveArgs moveArgs) =>
        {
            Console.Write($"Source={moveArgs.source}, Target={moveArgs.target}, Force={moveArgs.force}");
        });
    }
}
""";

        // Using "argument," in <param> doc marks constructor param as positional argument
        await verifier.Execute(code, "/src /dest --force", "Source=/src, Target=/dest, Force=True");
        await verifier.Execute(code, "/src /dest", "Source=/src, Target=/dest, Force=False");
    }

    [Test]
    public async Task ArgumentCommentWithPlainTripleSlash()
    {
        // language=csharp
        var code = """
using System;

public class GrepArgs
{
    /// argument, The search pattern
    public string Pattern { get; set; } = "";

    /// -r, Search recursively
    public bool Recursive { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] GrepArgs grepArgs) =>
        {
            Console.Write($"Pattern={grepArgs.Pattern}, Recursive={grepArgs.Recursive}");
        });
    }
}
""";

        // Plain triple slash comments with "argument," also work
        await verifier.Execute(code, "*.txt -r", "Pattern=*.txt, Recursive=True");
        await verifier.Execute(code, "*.log", "Pattern=*.log, Recursive=False");
    }
}
