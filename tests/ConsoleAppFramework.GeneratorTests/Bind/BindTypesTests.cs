namespace ConsoleAppFramework.GeneratorTests.Bind;

[ClassDataSource<VerifyHelper>]
public class BindTypesTests(VerifyHelper verifier)
{
    [Test]
    public async Task EnumProperty()
    {
        // language=csharp
        var code = """
using System;

public enum LogLevel { Debug, Info, Warning, Error }

public class LogConfig
{
    public LogLevel Level { get; set; } = LogLevel.Info;
    public string OutputPath { get; set; } = "log.txt";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] LogConfig config) =>
        {
            Console.Write($"Level={config.Level}, Output={config.OutputPath}");
        });
    }
}
""";

        await verifier.Execute(code, "--level Warning --output-path errors.log", "Level=Warning, Output=errors.log");
    }

    [Test]
    public async Task NullableProperties()
    {
        // language=csharp
        var code = """
#nullable enable
using System;

public class Config
{
    public string? OptionalHost { get; set; }
    public int? OptionalPort { get; set; }
    public string RequiredName { get; set; } = "default";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Host={config.OptionalHost ?? "null"}, Port={config.OptionalPort?.ToString() ?? "null"}, Name={config.RequiredName}");
        });
    }
}
""";

        await verifier.Execute(code, "--optional-host myhost --required-name test", "Host=myhost, Port=null, Name=test");
        await verifier.Execute(code, "", "Host=null, Port=null, Name=default");
    }

    [Test]
    public async Task DateTimeProperty()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public DateTime StartDate { get; set; } = DateTime.MinValue;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"StartDate={config.StartDate:yyyy-MM-dd}");
        });
    }
}
""";

        await verifier.Execute(code, "--start-date 2024-01-15", "StartDate=2024-01-15");
    }

    [Test]
    public async Task GuidProperty()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public Guid Id { get; set; } = Guid.Empty;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Id={config.Id}");
        });
    }
}
""";

        await verifier.Execute(code, "--id 12345678-1234-1234-1234-123456789abc", "Id=12345678-1234-1234-1234-123456789abc");
    }

    [Test]
    public async Task DecimalProperty()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public decimal Price { get; set; } = 0.0m;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Price={config.Price}");
        });
    }
}
""";

        await verifier.Execute(code, "--price 99.99", "Price=99.99");
    }

    [Test]
    public async Task FloatAndDoubleProperties()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public float FloatValue { get; set; } = 0.0f;
    public double DoubleValue { get; set; } = 0.0;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Float={config.FloatValue}, Double={config.DoubleValue}");
        });
    }
}
""";

        await verifier.Execute(code, "--float-value 3.14 --double-value 2.71828", "Float=3.14, Double=2.71828");
    }

    [Test]
    public async Task ByteAndShortProperties()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public byte ByteValue { get; set; } = 0;
    public sbyte SByteValue { get; set; } = 0;
    public short ShortValue { get; set; } = 0;
    public ushort UShortValue { get; set; } = 0;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Byte={config.ByteValue}, SByte={config.SByteValue}, Short={config.ShortValue}, UShort={config.UShortValue}");
        });
    }
}
""";

        // SByteValue -> s-byte-value, UShortValue -> u-short-value
        await verifier.Execute(code, "--byte-value 255 --s-byte-value -128 --short-value -32000 --u-short-value 65000", "Byte=255, SByte=-128, Short=-32000, UShort=65000");
    }

    [Test]
    public async Task CharProperty()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public char Delimiter { get; set; } = ',';
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Delimiter={config.Delimiter}");
        });
    }
}
""";

        await verifier.Execute(code, "--delimiter |", "Delimiter=|");
    }

    [Test]
    public async Task LongProperty()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public long BigNumber { get; set; } = 0;
    public ulong UnsignedBigNumber { get; set; } = 0;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Big={config.BigNumber}, UBig={config.UnsignedBigNumber}");
        });
    }
}
""";

        await verifier.Execute(code, "--big-number -9223372036854775808 --unsigned-big-number 18446744073709551615", "Big=-9223372036854775808, UBig=18446744073709551615");
    }

    [Test]
    public async Task TimeSpanProperty()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public TimeSpan Timeout { get; set; } = TimeSpan.Zero;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Timeout={config.Timeout}");
        });
    }
}
""";

        await verifier.Execute(code, "--timeout 00:05:30", "Timeout=00:05:30");
    }

    [Test]
    public async Task NullableEnumProperty()
    {
        // language=csharp
        var code = """
using System;

public enum LogLevel { Debug, Info, Warning, Error }

public class Config
{
    public LogLevel? Level { get; set; } = null;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Level={config.Level?.ToString() ?? "null"}");
        });
    }
}
""";

        await verifier.Execute(code, "--level Warning", "Level=Warning");
    }

    [Test]
    public async Task NullableEnumProperty_NotProvided()
    {
        // language=csharp
        var code = """
using System;

public enum LogLevel { Debug, Info, Warning, Error }

public class Config
{
    public LogLevel? Level { get; set; } = null;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Level={config.Level?.ToString() ?? "null"}");
        });
    }
}
""";

        await verifier.Execute(code, "", "Level=null");
    }

    // Note: Boolean with explicit true/false values (--verbose true/false) is not supported
    // in [Bind] mode. Options are treated as flags where presence means true.
    // Use regular parameters for explicit boolean value parsing.

    [Test]
    public async Task IntArrayProperty_CommaSeparated()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public int[] Ports { get; set; } = Array.Empty<int>();
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Ports={string.Join(",", config.Ports)}");
        });
    }
}
""";

        await verifier.Execute(code, "--ports 8080,8081,8082", "Ports=8080,8081,8082");
    }

    [Test]
    public async Task IntArrayProperty_SingleValue()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public int[] Ports { get; set; } = Array.Empty<int>();
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Ports={string.Join(",", config.Ports)}");
        });
    }
}
""";

        await verifier.Execute(code, "--ports 8080", "Ports=8080");
    }

    [Test]
    public async Task StringArrayProperty()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public string[] Tags { get; set; } = Array.Empty<string>();
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Tags={string.Join(";", config.Tags)}");
        });
    }
}
""";

        await verifier.Execute(code, "--tags alpha,beta,gamma", "Tags=alpha;beta;gamma");
    }

    [Test]
    public async Task BigIntegerProperty()
    {
        // language=csharp
        var code = """
using System;
using System.Numerics;

public class Config
{
    public BigInteger Value { get; set; } = BigInteger.Zero;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Value={config.Value}");
        });
    }
}
""";

        await verifier.Execute(code, "--value 12345678901234567890", "Value=12345678901234567890");
    }

    [Test]
    public async Task ArrayProperty_WithConstructor()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public int[] Values { get; set; }

    public Config(int[] values)
    {
        Values = values;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Values={string.Join(",", config.Values)}");
        });
    }
}
""";

        await verifier.Execute(code, "--values 1,2,3,4,5", "Values=1,2,3,4,5");
    }

    [Test]
    public async Task DoubleArrayProperty()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public double[] Coordinates { get; set; } = Array.Empty<double>();
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Coords={string.Join(";", config.Coordinates)}");
        });
    }
}
""";

        await verifier.Execute(code, "--coordinates 1.5,2.5,3.5", "Coords=1.5;2.5;3.5");
    }
}
