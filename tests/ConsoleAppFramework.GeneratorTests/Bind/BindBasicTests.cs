namespace ConsoleAppFramework.GeneratorTests.Bind;

[ClassDataSource<VerifyHelper>]
public class BindBasicTests(VerifyHelper verifier)
{
    [Test]
    public async Task BasicClassBinding()
    {
        // language=csharp
        var code = """
using System;

public class DatabaseConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "mydb";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] DatabaseConfig config) =>
        {
            Console.Write($"{config.Host}:{config.Port}/{config.Database}");
        });
    }
}
""";

        await verifier.Execute(code, "--host myhost --port 3306 --database testdb", "myhost:3306/testdb");
    }

    [Test]
    public async Task BasicClassBindingWithDefaults()
    {
        // language=csharp
        var code = """
using System;

public class DatabaseConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "mydb";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] DatabaseConfig config) =>
        {
            Console.Write($"{config.Host}:{config.Port}/{config.Database}");
        });
    }
}
""";

        // Only override Host, use defaults for others
        await verifier.Execute(code, "--host myhost", "myhost:5432/mydb");
    }

    [Test]
    public async Task CustomPrefix()
    {
        // language=csharp
        var code = """
using System;

public class ServerConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 8080;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind(Prefix = "server")] ServerConfig config) =>
        {
            Console.Write($"{config.Host}:{config.Port}");
        });
    }
}
""";

        await verifier.Execute(code, "--server-host myhost --server-port 9000", "myhost:9000");
    }

    [Test]
    public async Task CaseInsensitiveMatching()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public string Host { get; set; } = "localhost";
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

        // Test case-insensitive matching
        await verifier.Execute(code, "--HOST myhost --PORT 9000", "myhost:9000");
    }
}
