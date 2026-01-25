namespace ConsoleAppFramework.GeneratorTests.Bind;

[ClassDataSource<VerifyHelper>]
public class BindXmlDocTests(VerifyHelper verifier)
{
    [Test]
    public async Task PropertyXmlDocInHelp()
    {
        // language=csharp
        var code = """
using System;

public class ServerConfig
{
    /// The hostname to connect to.
    public string Host { get; set; } = "localhost";

    /// <summary>-p|--port, The port number for the connection.</summary>
    public int Port { get; set; } = 8080;

    /// <summary>Enable verbose logging.</summary>
    public bool Verbose { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Log = x => Console.WriteLine(x);
        ConsoleApp.Run(args, ([Bind] ServerConfig config) =>
        {
            Console.Write($"{config.Host}:{config.Port}");
        });
    }
}
""";

        var expected = """
Usage: [options...] [-h|--help] [--version]

Options:
  --host <string>     The hostname to connect to. [Default: localhost]
  -p, --port <int>    The port number for the connection. [Default: 8080]
  --verbose           Enable verbose logging.

""";

        var (stdout, _) = verifier.Error(code, "--help");
        await Assert.That(stdout).IsEqualTo(expected);
    }

    [Test]
    public async Task ConstructorParamXmlDocInHelp()
    {
        // language=csharp
        var code = """
using System;

/// <summary>Search options for file operations.</summary>
/// <param name="pattern">The search pattern to match files.</param>
/// <param name="recursive">-r|--recursive, Search in subdirectories.</param>
public record SearchOptions(string pattern, bool recursive = false);

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Log = x => Console.WriteLine(x);
        ConsoleApp.Run(args, ([Bind] SearchOptions opts) =>
        {
            Console.Write($"{opts.pattern}:{opts.recursive}");
        });
    }
}
""";

        var expected = """
Usage: [options...] [-h|--help] [--version]

Options:
  --pattern <string>    The search pattern to match files. [Required]
  -r, --recursive       Search in subdirectories.

""";

        var (stdout, _) = verifier.Error(code, "--help");
        await Assert.That(stdout).IsEqualTo(expected);
    }

    [Test]
    public async Task MixedXmlDocInHelp()
    {
        // language=csharp
        var code = """
using System;

/// <summary>Copy operation options.</summary>
/// <param name="source">-s|--source, The source file path.</param>
/// <param name="destination">-d|--destination, The destination file path.</param>
public class CopyOptions
{
    public CopyOptions(string source, string destination)
    {
        Source = source;
        Destination = destination;
    }

    public string Source { get; }
    public string Destination { get; }

    /// <summary>-f|--force, Overwrite existing files.</summary>
    public bool Force { get; set; } = false;

    /// <summary>-v|--verbose, Show detailed progress.</summary>
    public bool Verbose { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Log = x => Console.WriteLine(x);
        ConsoleApp.Run(args, ([Bind] CopyOptions opts) =>
        {
            Console.Write($"{opts.Source}->{opts.Destination}");
        });
    }
}
""";

        var expected = """
Usage: [options...] [-h|--help] [--version]

Options:
  -s, --source <string>         The source file path. [Required]
  -d, --destination <string>    The destination file path. [Required]
  -f, --force                   Overwrite existing files.
  -v, --verbose                 Show detailed progress.

""";

        var (stdout, _) = verifier.Error(code, "--help");
        await Assert.That(stdout).IsEqualTo(expected);
    }

    [Test]
    public async Task NoXmlDocStillWorks()
    {
        // language=csharp
        var code = """
using System;

public class SimpleConfig
{
    public string Name { get; set; } = "default";
    public int Count { get; set; } = 0;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] SimpleConfig config) =>
        {
            Console.Write($"{config.Name}:{config.Count}");
        });
    }
}
""";

        // Test that execution still works without XML docs
        await verifier.Execute(code, "--name test --count 5", "test:5");
    }

    [Test]
    public async Task ShortOptionAliases()
    {
        // language=csharp
        var code = """
using System;

public class AliasConfig
{
    /// <summary>-h|--host, The hostname to connect to</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>-p|--port, The port number</summary>
    public int Port { get; set; } = 8080;

    /// <summary>-v|--verbose, Enable verbose mode</summary>
    public bool Verbose { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] AliasConfig config) =>
        {
            Console.Write($"{config.Host}:{config.Port}:{config.Verbose}");
        });
    }
}
""";

        // Test that short options work
        await verifier.Execute(code, "-h myhost -p 9000 -v", "myhost:9000:True");
    }

    [Test]
    public async Task ShortOptionAliasesInHelp()
    {
        // language=csharp
        var code = """
using System;

public class AliasConfig
{
    /// <summary>-h|--host, The hostname to connect to</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>-p|--port, The port number</summary>
    public int Port { get; set; } = 8080;

    /// <summary>-v|--verbose, Enable verbose mode</summary>
    public bool Verbose { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Log = x => Console.WriteLine(x);
        ConsoleApp.Run(args, ([Bind] AliasConfig config) =>
        {
            Console.Write($"{config.Host}:{config.Port}");
        });
    }
}
""";

        var expected = """
Usage: [options...] [-h|--help] [--version]

Options:
  -h, --host <string>    The hostname to connect to [Default: localhost]
  -p, --port <int>       The port number [Default: 8080]
  -v, --verbose          Enable verbose mode

""";

        var (stdout, _) = verifier.Error(code, "--help");
        await Assert.That(stdout).IsEqualTo(expected);
    }

    [Test]
    public async Task ArgumentCommentProperty()
    {
        // language=csharp
        var code = """
using System;

public class CopyConfig
{
    /// <summary>argument, The source file path</summary>
    public string Source { get; set; } = "";

    /// <summary>argument, The destination file path</summary>
    public string Destination { get; set; } = "";

    /// <summary>-f|--force, Force overwrite</summary>
    public bool Force { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] CopyConfig config) =>
        {
            Console.Write($"{config.Source}->{config.Destination}:{config.Force}");
        });
    }
}
""";

        // Properties with "argument," in comment should be treated as positional arguments
        await verifier.Execute(code, "/source/file /dest/file -f", "/source/file->/dest/file:True");
    }

    [Test]
    public async Task ArgumentCommentPropertyInHelp()
    {
        // language=csharp
        var code = """
using System;

public class CopyConfig
{
    /// <summary>argument, The source file path</summary>
    public string Source { get; set; } = "";

    /// <summary>argument, The destination file path</summary>
    public string Destination { get; set; } = "";

    /// <summary>-f|--force, Force overwrite</summary>
    public bool Force { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Log = x => Console.WriteLine(x);
        ConsoleApp.Run(args, ([Bind] CopyConfig config) =>
        {
            Console.Write($"{config.Source}->{config.Destination}:{config.Force}");
        });
    }
}
""";

        var expected = """
Usage: [arguments...] [options...] [-h|--help] [--version]

Arguments:
  [0] <string>    The source file path
  [1] <string>    The destination file path

Options:
  -f, --force    Force overwrite

""";

        var (stdout, _) = verifier.Error(code, "--help");
        await Assert.That(stdout).IsEqualTo(expected);
    }

    [Test]
    public async Task ArgumentCommentConstructorParam()
    {
        // language=csharp
        var code = """
using System;

/// <summary>Copy options</summary>
/// <param name="source">argument, The source file</param>
/// <param name="destination">argument, The destination file</param>
public record CopyOptions(string source, string destination)
{
    public bool Force { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] CopyOptions opts) =>
        {
            Console.Write($"{opts.source}->{opts.destination}:{opts.Force}");
        });
    }
}
""";

        // Constructor params with "argument," in comment should be treated as positional arguments
        await verifier.Execute(code, "/src /dst --force", "/src->/dst:True");
    }

    [Test]
    public async Task ArgumentCommentConstructorParamInHelp()
    {
        // language=csharp
        var code = """
using System;

/// <summary>Copy options</summary>
/// <param name="source">argument, The source file</param>
/// <param name="destination">argument, The destination file</param>
public record CopyOptions(string source, string destination)
{
    /// <summary>-f|--force, Force overwrite</summary>
    public bool Force { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Log = x => Console.WriteLine(x);
        ConsoleApp.Run(args, ([Bind] CopyOptions opts) =>
        {
            Console.Write($"{opts.source}->{opts.destination}:{opts.Force}");
        });
    }
}
""";

        var expected = """
Usage: [arguments...] [options...] [-h|--help] [--version]

Arguments:
  [0] <string>    The source file
  [1] <string>    The destination file

Options:
  -f, --force    Force overwrite

""";

        var (stdout, _) = verifier.Error(code, "--help");
        await Assert.That(stdout).IsEqualTo(expected);
    }

    [Test]
    public async Task MixedAliasesAndArguments()
    {
        // language=csharp
        var code = """
using System;

public class GrepConfig
{
    /// <summary>argument, The search pattern</summary>
    public string Pattern { get; set; } = "";

    /// <summary>-i|--ignore-case, Case insensitive matching</summary>
    public bool IgnoreCase { get; set; } = false;

    /// <summary>-r|--recursive, Recursive search</summary>
    public bool Recursive { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] GrepConfig config) =>
        {
            Console.Write($"pattern={config.Pattern},i={config.IgnoreCase},r={config.Recursive}");
        });
    }
}
""";

        // Mix of positional argument and short options
        await verifier.Execute(code, "*.txt -i -r", "pattern=*.txt,i=True,r=True");
    }

    [Test]
    public async Task MixedAliasesAndArgumentsInHelp()
    {
        // language=csharp
        var code = """
using System;

public class GrepConfig
{
    /// <summary>argument, The search pattern</summary>
    public string Pattern { get; set; } = "";

    /// <summary>-i|--ignore-case, Case insensitive matching</summary>
    public bool IgnoreCase { get; set; } = false;

    /// <summary>-r|--recursive, Recursive search</summary>
    public bool Recursive { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Log = x => Console.WriteLine(x);
        ConsoleApp.Run(args, ([Bind] GrepConfig config) =>
        {
            Console.Write($"pattern={config.Pattern},i={config.IgnoreCase},r={config.Recursive}");
        });
    }
}
""";

        var expected = """
Usage: [arguments...] [options...] [-h|--help] [--version]

Arguments:
  [0] <string>    The search pattern

Options:
  -i, --ignore-case    Case insensitive matching
  -r, --recursive      Recursive search

""";

        var (stdout, _) = verifier.Error(code, "--help");
        await Assert.That(stdout).IsEqualTo(expected);
    }

    [Test]
    public async Task ArgumentCommentCaseInsensitive()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    /// ARGUMENT, The file to process
    public string File { get; set; } = "";

    /// Argument, The path to use
    public string Path { get; set; } = "";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"{config.File}|{config.Path}");
        });
    }
}
""";

        // Case insensitive "argument," in comment
        await verifier.Execute(code, "file.txt /path/to/dir", "file.txt|/path/to/dir");
    }

    [Test]
    public async Task ThreeAliases()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    /// <summary>-h|--host|--hostname, The target hostname</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>-p|--port|--server-port, The port number</summary>
    public int Port { get; set; } = 8080;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"{config.Host}:{config.Port}");
        });
    }
}
""";

        // Test all three aliases work
        await verifier.Execute(code, "-h myhost -p 9000", "myhost:9000");
        await verifier.Execute(code, "--host anotherhost --port 8000", "anotherhost:8000");
        await verifier.Execute(code, "--hostname third --server-port 7000", "third:7000");
    }

    [Test]
    public async Task ThreeAliasesInHelp()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    /// <summary>-h|--host|--hostname, The target hostname</summary>
    public string Host { get; set; } = "localhost";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Log = x => Console.WriteLine(x);
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"{config.Host}");
        });
    }
}
""";

        var expected = """
Usage: [options...] [-h|--help] [--version]

Options:
  -h, --host, --hostname <string>    The target hostname [Default: localhost]

""";

        var (stdout, _) = verifier.Error(code, "--help");
        await Assert.That(stdout).IsEqualTo(expected);
    }
}
