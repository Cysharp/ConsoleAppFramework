namespace ConsoleAppFramework.GeneratorTests.Bind;

[ClassDataSource<VerifyHelper>]
public class BindValidationTests(VerifyHelper verifier)
{
    // Note: Validation attributes like [Range] are not yet applied to [Bind] types
    // Future feature: RangeValidation_OnIntProperty tests

    [Test]
    public async Task RequiredProperty_Missing_Error()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public required string Name { get; set; }
    public int Port { get; set; } = 8080;
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

        // Not providing the required property should fail
        var (stdout, exitCode) = verifier.Error(code, "--port 9000");
        await Assert.That(exitCode).IsNotEqualTo(0);
    }

    [Test]
    public async Task RequiredProperty_Provided_Success()
    {
        // language=csharp
        var code = """
using System;

public class Config
{
    public required string Name { get; set; }
    public int Port { get; set; } = 8080;
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

        // Providing the required property should succeed
        await verifier.Execute(code, "--name test --port 9000", "name=test, port=9000");
    }
}
