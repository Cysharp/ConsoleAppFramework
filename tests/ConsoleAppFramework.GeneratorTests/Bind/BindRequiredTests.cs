namespace ConsoleAppFramework.GeneratorTests.Bind;

[ClassDataSource<VerifyHelper>]
public class BindRequiredTests(VerifyHelper verifier)
{
    [Test]
    public async Task RequiredInitProperty_MustBeSpecified()
    {
        // language=csharp
        var code = """
using System;

public record Config(string Name)
{
    public required string ApiKey { get; init; }
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Name={config.Name}, ApiKey={config.ApiKey}");
        });
    }
}
""";

        await verifier.Execute(code, "--name test --api-key secret123", "Name=test, ApiKey=secret123");
    }

    [Test]
    public async Task RequiredInitProperty_ErrorWhenMissing()
    {
        // language=csharp
        var code = """
using System;

public record Config(string Name)
{
    public required string ApiKey { get; init; }
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Name={config.Name}, ApiKey={config.ApiKey}");
        });
    }
}
""";

        // Missing required property should fail (error messages go to stderr, not captured)
        var (_, exitCode) = verifier.Error(code, "--name test");
        await Assert.That(exitCode).IsNotEqualTo(0);
    }

    [Test]
    public async Task ConstructorParameter_WithoutDefault_IsRequired()
    {
        // language=csharp
        var code = """
using System;

public record Config(string Name, string Environment);

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Name={config.Name}, Env={config.Environment}");
        });
    }
}
""";

        // Both constructor params without defaults are required
        await verifier.Execute(code, "--name myapp --environment prod", "Name=myapp, Env=prod");

        // Missing Name should fail
        var (_, exitCode1) = verifier.Error(code, "--environment prod");
        await Assert.That(exitCode1).IsNotEqualTo(0);

        // Missing Environment should fail
        var (_, exitCode2) = verifier.Error(code, "--name myapp");
        await Assert.That(exitCode2).IsNotEqualTo(0);

        // Missing both should fail
        var (_, exitCode3) = verifier.Error(code, "");
        await Assert.That(exitCode3).IsNotEqualTo(0);
    }

    [Test]
    public async Task ConstructorParameter_WithDefault_IsOptional()
    {
        // language=csharp
        var code = """
using System;

public record Config(string Name, string Environment = "development", int Port = 8080);

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Name={config.Name}, Env={config.Environment}, Port={config.Port}");
        });
    }
}
""";

        // Name is required (no default), others are optional
        await verifier.Execute(code, "--name myapp", "Name=myapp, Env=development, Port=8080");
        await verifier.Execute(code, "--name myapp --environment prod", "Name=myapp, Env=prod, Port=8080");
        await verifier.Execute(code, "--name myapp --port 9000", "Name=myapp, Env=development, Port=9000");

        // Missing required Name should fail
        var (_, exitCode) = verifier.Error(code, "--environment prod --port 9000");
        await Assert.That(exitCode).IsNotEqualTo(0);
    }

    [Test]
    public async Task MixedConstructorAndInitProperties_RequiredValidation()
    {
        // language=csharp
        var code = """
using System;

public record ServiceConfig(string ServiceName, int Port = 8080)
{
    public required string ApiKey { get; init; }
    public string Region { get; init; } = "us-east-1";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] ServiceConfig config) =>
        {
            Console.Write($"Name={config.ServiceName}, Port={config.Port}, Key={config.ApiKey}, Region={config.Region}");
        });
    }
}
""";

        // ServiceName (ctor, no default) and ApiKey (required init) are both required
        await verifier.Execute(code, "--service-name myapp --api-key secret", "Name=myapp, Port=8080, Key=secret, Region=us-east-1");

        // Missing ServiceName should fail
        var (_, exitCode1) = verifier.Error(code, "--api-key secret");
        await Assert.That(exitCode1).IsNotEqualTo(0);

        // Missing ApiKey should fail
        var (_, exitCode2) = verifier.Error(code, "--service-name myapp");
        await Assert.That(exitCode2).IsNotEqualTo(0);

        // Missing both required should fail
        var (_, exitCode3) = verifier.Error(code, "--port 9000 --region eu-west-1");
        await Assert.That(exitCode3).IsNotEqualTo(0);
    }

    [Test]
    public async Task OptionalInitProperty_PreservesDefault()
    {
        // language=csharp
        var code = """
using System;

public record Config(string Name)
{
    public string Description { get; init; } = "default-description";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Name={config.Name}, Desc={config.Description}");
        });
    }
}
""";

        // Without specifying description, it keeps the class default
        await verifier.Execute(code, "--name test", "Name=test, Desc=default-description");
        // With description specified, it uses the provided value
        await verifier.Execute(code, "--name test --description custom", "Name=test, Desc=custom");
    }

    [Test]
    public async Task MixedRequiredAndOptionalInitProperties()
    {
        // language=csharp
        var code = """
using System;

public record Config(string Name)
{
    public required string ApiKey { get; init; }
    public string Environment { get; init; } = "production";
    public int Timeout { get; init; } = 30;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Name={config.Name}, ApiKey={config.ApiKey}, Env={config.Environment}, Timeout={config.Timeout}");
        });
    }
}
""";

        // Only required property specified - optionals keep defaults
        await verifier.Execute(code, "--name app --api-key key123", "Name=app, ApiKey=key123, Env=production, Timeout=30");
        // Override one optional
        await verifier.Execute(code, "--name app --api-key key123 --environment staging", "Name=app, ApiKey=key123, Env=staging, Timeout=30");
        // Override all
        await verifier.Execute(code, "--name app --api-key key123 --environment dev --timeout 60", "Name=app, ApiKey=key123, Env=dev, Timeout=60");
    }

    [Test]
    public async Task RequiredPropertyOnClass()
    {
        // language=csharp
        var code = """
using System;

public class DatabaseConfig
{
    public required string ConnectionString { get; set; }
    public int MaxConnections { get; set; } = 10;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] DatabaseConfig config) =>
        {
            Console.Write($"CS={config.ConnectionString}, Max={config.MaxConnections}");
        });
    }
}
""";

        await verifier.Execute(code, "--connection-string localhost:5432", "CS=localhost:5432, Max=10");
        await verifier.Execute(code, "--connection-string localhost:5432 --max-connections 50", "CS=localhost:5432, Max=50");
    }

    [Test]
    public async Task RequiredPropertyOnClass_ErrorWhenMissing()
    {
        // language=csharp
        var code = """
using System;

public class DatabaseConfig
{
    public required string ConnectionString { get; set; }
    public int MaxConnections { get; set; } = 10;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] DatabaseConfig config) =>
        {
            Console.Write($"CS={config.ConnectionString}, Max={config.MaxConnections}");
        });
    }
}
""";

        // Missing required property should fail (error messages go to stderr, not captured)
        var (_, exitCode) = verifier.Error(code, "--max-connections 50");
        await Assert.That(exitCode).IsNotEqualTo(0);
    }

    [Test]
    public async Task MultipleRequiredProperties()
    {
        // language=csharp
        var code = """
using System;

public record Credentials
{
    public required string Username { get; init; }
    public required string Password { get; init; }
    public string Domain { get; init; } = "default";
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Credentials creds) =>
        {
            Console.Write($"User={creds.Username}, Pass={creds.Password}, Domain={creds.Domain}");
        });
    }
}
""";

        await verifier.Execute(code, "--username admin --password secret", "User=admin, Pass=secret, Domain=default");
        await verifier.Execute(code, "--username admin --password secret --domain corp", "User=admin, Pass=secret, Domain=corp");
    }

    [Test]
    public async Task RequiredWithConstructorParameters()
    {
        // language=csharp
        var code = """
using System;

public record ServiceConfig(string ServiceName, int Port = 8080)
{
    public required string ApiKey { get; init; }
    public bool EnableLogging { get; init; } = true;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] ServiceConfig config) =>
        {
            Console.Write($"Service={config.ServiceName}, Port={config.Port}, Key={config.ApiKey}, Log={config.EnableLogging}");
        });
    }
}
""";

        // ServiceName is required (ctor param without default), ApiKey is required (required modifier)
        // EnableLogging keeps its default (true) when not specified
        await verifier.Execute(code, "--service-name myapp --api-key abc123", "Service=myapp, Port=8080, Key=abc123, Log=True");
    }

    [Test]
    public async Task BoolInitProperty_PreservesDefaults()
    {
        // language=csharp
        var code = """
using System;

public record FeatureFlags(string AppName)
{
    public bool EnableMetrics { get; init; } = true;
    public bool EnableTracing { get; init; } = true;
    public bool EnableCaching { get; init; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] FeatureFlags flags) =>
        {
            Console.Write($"Metrics={flags.EnableMetrics}, Tracing={flags.EnableTracing}, Caching={flags.EnableCaching}");
        });
    }
}
""";

        // All keep defaults (true, true, false)
        await verifier.Execute(code, "--app-name myapp", "Metrics=True, Tracing=True, Caching=False");
        // Boolean flags: presence sets to true, cannot set to false via CLI
        // Enable caching (was false by default)
        await verifier.Execute(code, "--app-name myapp --enable-caching", "Metrics=True, Tracing=True, Caching=True");
    }

    [Test]
    public async Task NullableInitProperty_PreservesNull()
    {
        // language=csharp
        var code = """
#nullable enable
using System;

public record Config(string Name)
{
    public string? OptionalTag { get; init; }
    public int? OptionalCount { get; init; }
}

public class Program
{
    public static void Main(string[] args)
    {
        ConsoleApp.Run(args, ([Bind] Config config) =>
        {
            Console.Write($"Name={config.Name}, Tag={config.OptionalTag ?? "null"}, Count={config.OptionalCount?.ToString() ?? "null"}");
        });
    }
}
""";

        // Nullable properties stay null when not specified
        await verifier.Execute(code, "--name test", "Name=test, Tag=null, Count=null");
        // Can be set
        await verifier.Execute(code, "--name test --optional-tag mytag --optional-count 42", "Name=test, Tag=mytag, Count=42");
    }
}
