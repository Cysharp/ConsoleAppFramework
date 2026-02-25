namespace ConsoleAppFramework.GeneratorTests.Bind;

[ClassDataSource<VerifyHelper>]
public class BindAdvancedTests(VerifyHelper verifier)
{
    [Test]
    public async Task MixedParametersWithBind()
    {
        // language=csharp
        var code = """
using System;

public record Options(bool Verbose = false, bool DryRun = false);

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, (
            string name,
            int count,
            [Bind] Options options
        ) =>
        {
            Console.Write($"name={name}, count={count}, verbose={options.Verbose}, dryRun={options.DryRun}");
        });
    }
}
""";

        await verifier.Execute(code, "--name test --count 5 --verbose", "name=test, count=5, verbose=True, dryRun=False");
    }

    [Test]
    public async Task MultipleBindParameters()
    {
        // language=csharp
        var code = """
using System;

public class DatabaseConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
}

public class CacheConfig
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6379;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, (
            [Bind] DatabaseConfig database,
            [Bind] CacheConfig cache
        ) =>
        {
            Console.Write($"DB={database.Host}:{database.Port}, Cache={cache.Host}:{cache.Port}");
        });
    }
}
""";

        await verifier.Execute(code, "--database-host db.example.com --cache-host cache.example.com --cache-port 6380", "DB=db.example.com:5432, Cache=cache.example.com:6380");
    }

    [Test]
    public async Task MultipleBinds_DifferentPrefixes_SamePropertyName()
    {
        // language=csharp
        var code = """
using System;

public class DbConfig
{
    public string ConnectionString { get; set; } = "";
    public int Timeout { get; set; } = 30;
}

public class CacheConfig
{
    public string ConnectionString { get; set; } = "";
    public int Timeout { get; set; } = 10;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, (
            [Bind(Prefix = "db")] DbConfig db,
            [Bind(Prefix = "cache")] CacheConfig cache
        ) =>
        {
            Console.Write($"DB={db.ConnectionString}:{db.Timeout}, Cache={cache.ConnectionString}:{cache.Timeout}");
        });
    }
}
""";

        await verifier.Execute(code, "--db-connection-string server=db --db-timeout 60 --cache-connection-string server=cache --cache-timeout 5", "DB=server=db:60, Cache=server=cache:5");
    }

    [Test]
    public async Task BindWithRegularParameters()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public bool Verbose { get; set; } = false;
    public string Format { get; set; } = "json";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, (
            [Argument] string input,
            [Argument] string output,
            [Bind] Config config
        ) =>
        {
            Console.Write($"input={input}, output={output}, verbose={config.Verbose}, format={config.Format}");
        });
    }
}
""";

        await verifier.Execute(code, "in.txt out.txt --verbose --format xml", "input=in.txt, output=out.txt, verbose=True, format=xml");
    }
}
