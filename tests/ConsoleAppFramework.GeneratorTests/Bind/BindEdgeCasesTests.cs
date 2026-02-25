namespace ConsoleAppFramework.GeneratorTests.Bind;

[ClassDataSource<VerifyHelper>]
public class BindEdgeCasesTests(VerifyHelper verifier)
{
    [Test]
    public async Task DeepInheritance_ThreeLevels()
    {
        // language=csharp
        var code = """
using System;

public class BaseOptions
{
    public bool Verbose { get; set; } = false;
}

public class MiddleOptions : BaseOptions
{
    public string Format { get; set; } = "json";
}

public class FinalOptions : MiddleOptions
{
    public string Output { get; set; } = "stdout";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] FinalOptions options) =>
        {
            Console.Write($"verbose={options.Verbose}, format={options.Format}, output={options.Output}");
        });
    }
}
""";

        await verifier.Execute(code, "--verbose --format xml --output file.txt", "verbose=True, format=xml, output=file.txt");
    }

    [Test]
    public async Task DeepInheritance_ThreeLevels_DefaultValues()
    {
        // language=csharp
        var code = """
using System;

public class BaseOptions
{
    public bool Debug { get; set; } = true;
}

public class MiddleOptions : BaseOptions
{
    public int Level { get; set; } = 5;
}

public class FinalOptions : MiddleOptions
{
    public string Mode { get; set; } = "auto";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] FinalOptions options) =>
        {
            Console.Write($"debug={options.Debug}, level={options.Level}, mode={options.Mode}");
        });
    }
}
""";

        // Using all defaults
        await verifier.Execute(code, "", "debug=True, level=5, mode=auto");
    }

    [Test]
    public async Task EmptyBindClass_AllDefaults()
    {
        // language=csharp
        var code = """
using System;

public class EmptyConfig
{
    // No properties - but still valid
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] EmptyConfig config) =>
        {
            Console.Write($"config is not null: {config != null}");
        });
    }
}
""";

        await verifier.Execute(code, "", "config is not null: True");
    }

    [Test]
    public async Task InvalidStringToInt_ErrorCase()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public int Port { get; set; } = 8080;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"port={config.Port}");
        });
    }
}
""";

        // Pass a non-numeric value for an int property - should fail parsing
        var (stdout, exitCode) = verifier.Error(code, "--port not-a-number");
        await Assert.That(exitCode).IsNotEqualTo(0);
    }

    [Test]
    public async Task EnumProperty_ValidValue()
    {
        // language=csharp
        var code = """
using System;

public enum LogLevel { Debug, Info, Warning, Error }

public class Config
{
    public LogLevel Level { get; set; } = LogLevel.Info;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"level={config.Level}");
        });
    }
}
""";

        await verifier.Execute(code, "--level Error", "level=Error");
    }

    [Test]
    public async Task EnumProperty_InvalidValue_ErrorCase()
    {
        // language=csharp
        var code = """
using System;

public enum LogLevel { Debug, Info, Warning, Error }

public class Config
{
    public LogLevel Level { get; set; } = LogLevel.Info;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"level={config.Level}");
        });
    }
}
""";

        // Pass an invalid enum value - should fail parsing
        var (stdout, exitCode) = verifier.Error(code, "--level NotAValidLevel");
        await Assert.That(exitCode).IsNotEqualTo(0);
    }

    // Note: Array properties are not yet supported in [Bind] types
    // Future feature: ArrayProperty_MultipleValues test

    [Test]
    public async Task NullableIntProperty()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public int? OptionalCount { get; set; } = null;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"count={config.OptionalCount?.ToString() ?? "null"}");
        });
    }
}
""";

        // Without the optional value
        await verifier.Execute(code, "", "count=null");
    }

    [Test]
    public async Task NullableIntProperty_WithValue()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public int? OptionalCount { get; set; } = null;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"count={config.OptionalCount?.ToString() ?? "null"}");
        });
    }
}
""";

        // With the optional value
        await verifier.Execute(code, "--optional-count 42", "count=42");
    }

    [Test]
    public async Task SinglePropertyClass()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public string Name { get; set; } = "default";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"name={config.Name}");
        });
    }
}
""";

        await verifier.Execute(code, "--name test", "name=test");
    }

    [Test]
    public async Task ManyProperties_TenPlus()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public string Prop1 { get; set; } = "a";
    public string Prop2 { get; set; } = "b";
    public string Prop3 { get; set; } = "c";
    public string Prop4 { get; set; } = "d";
    public string Prop5 { get; set; } = "e";
    public string Prop6 { get; set; } = "f";
    public string Prop7 { get; set; } = "g";
    public string Prop8 { get; set; } = "h";
    public string Prop9 { get; set; } = "i";
    public string Prop10 { get; set; } = "j";
    public string Prop11 { get; set; } = "k";
    public string Prop12 { get; set; } = "l";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"{config.Prop1},{config.Prop2},{config.Prop3},{config.Prop4},{config.Prop5},{config.Prop6},{config.Prop7},{config.Prop8},{config.Prop9},{config.Prop10},{config.Prop11},{config.Prop12}");
        });
    }
}
""";

        await verifier.Execute(code, "--prop1 1 --prop2 2 --prop3 3 --prop4 4 --prop5 5 --prop6 6 --prop7 7 --prop8 8 --prop9 9 --prop10 10 --prop11 11 --prop12 12", "1,2,3,4,5,6,7,8,9,10,11,12");
    }

    [Test]
    public async Task StructType_Works()
    {
        // language=csharp
        var code = """
using System;

public struct Config
{
    public string Name { get; set; }
    public int Port { get; set; }
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"name={config.Name}, port={config.Port}");
        });
    }
}
""";

        await verifier.Execute(code, "--name test --port 8080", "name=test, port=8080");
    }

    [Test]
    public async Task RecordStructType_Works()
    {
        // language=csharp
        var code = """
using System;

public record struct Config(string Name = "default", int Port = 8080);

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"name={config.Name}, port={config.Port}");
        });
    }
}
""";

        await verifier.Execute(code, "--name test --port 9000", "name=test, port=9000");
    }

    [Test]
    public async Task AcronymPropertyName_CorrectKebabCase()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public int HTTPPort { get; set; } = 80;
    public string XMLPath { get; set; } = "";
    public bool SSLEnabled { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"port={config.HTTPPort}, xml={config.XMLPath}, ssl={config.SSLEnabled}");
        });
    }
}
""";

        // Consecutive capitals stay together until the last one before lowercase
        // HTTPPort -> http-port, XMLPath -> xml-path, SSLEnabled -> ssl-enabled
        await verifier.Execute(code, "--http-port 443 --xml-path /data --ssl-enabled", "port=443, xml=/data, ssl=True");
    }

    [Test]
    public async Task UnderscorePropertyName()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public string Log_Level { get; set; } = "info";
    public int Max_Retries { get; set; } = 3;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"level={config.Log_Level}, retries={config.Max_Retries}");
        });
    }
}
""";

        // Properties with underscores - underscore is preserved, dash added before capitals
        // Log_Level -> log_-level, Max_Retries -> max_-retries
        await verifier.Execute(code, "--log_-level debug --max_-retries 5", "level=debug, retries=5");
    }

    [Test]
    public async Task PropertyNamedHelp_Works()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public string HelpText { get; set; } = "";
    public bool ShowVersion { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"help={config.HelpText}, version={config.ShowVersion}");
        });
    }
}
""";

        await verifier.Execute(code, "--help-text readme --show-version", "help=readme, version=True");
    }

    [Test]
    public async Task DeepInheritance_FourLevels()
    {
        // language=csharp
        var code = """
using System;

public class Level1
{
    public bool Verbose { get; set; } = false;
}

public class Level2 : Level1
{
    public string Format { get; set; } = "json";
}

public class Level3 : Level2
{
    public string Output { get; set; } = "stdout";
}

public class Level4 : Level3
{
    public int Count { get; set; } = 1;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Level4 options) =>
        {
            Console.Write($"verbose={options.Verbose}, format={options.Format}, output={options.Output}, count={options.Count}");
        });
    }
}
""";

        await verifier.Execute(code, "--verbose --format xml --output file.txt --count 5", "verbose=True, format=xml, output=file.txt, count=5");
    }
}
