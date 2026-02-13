namespace ConsoleAppFramework.GeneratorTests;

[ClassDataSource<VerifyHelper>]
public class AsParametersTest(VerifyHelper verifier)
{
    [Test]
    public async Task BasicFlattenAndInvoke()
    {
        await verifier.Execute("""
ConsoleApp.Run(args, ([AsParameters] CreateUserOptions options) =>
{
    Console.Write($"{options.Name}:{options.Age}:{options.Level}");
});

public record class CreateUserOptions(
    string Name,
    [Argument] int Age = 20,
    int Level = 1);
""", "--name Alice 33 --level 4", "Alice:33:4");
    }

    [Test]
    public async Task DefaultsAndNullableDefaults()
    {
        await verifier.Execute("""
ConsoleApp.Run(args, ([AsParameters] Options options) =>
{
    Console.Write($"{options.Name}:{(options.Level.HasValue ? options.Level.Value.ToString() : "null")}");
});

public record class Options(
    string Name = "anon",
    int? Level = null);
""", "", "anon:null");
    }

    [Test]
    public async Task ValidationFromConstructorParameter()
    {
        var (Stdout, ExitCode) = verifier.Error("""
ConsoleApp.Log = x => Console.Write(x);
ConsoleApp.Run(args, ([AsParameters] Options options) => Console.Write("OK"));

public record class Options(
    [Range(1, 10)] int Level);
""", "--level 42");

        await Assert.That(Stdout).Contains("between 1 and 10");
        await Assert.That(ExitCode).IsEqualTo(1);
    }

    [Test]
    public async Task ConstructorFromServices()
    {
        await verifier.Execute("""
var di = new MiniDI();
di.Register(typeof(MyService), new MyService("svc"));
ConsoleApp.ServiceProvider = di;

ConsoleApp.Run(args, ([AsParameters] Options options) =>
{
    Console.Write($"{options.Service.Name}:{options.Count}");
});

public record class Options(
    [FromServices] MyService Service,
    int Count);

public class MyService(string name)
{
    public string Name => name;
}

class MiniDI : IServiceProvider
{
    System.Collections.Generic.Dictionary<Type, object> dict = new();

    public void Register(Type type, object instance)
    {
        dict[type] = instance;
    }

    public object? GetService(Type serviceType)
    {
        return dict.TryGetValue(serviceType, out var instance) ? instance : null;
    }
}
""", "--count 9", "svc:9");
    }

    [Test]
    public async Task HelpParityWithEquivalentExpandedCommand()
    {
        var (Stdout, ExitCode) = verifier.Error("""
ConsoleApp.Log = x => Console.WriteLine(x);
ConsoleApp.Run(args, ([AsParameters] Options options) => { });

public record class Options(
    string Name,
    [Argument] int Age = 20,
    bool Force = false);
""", "--help");

        var expanded = verifier.Error("""
ConsoleApp.Log = x => Console.WriteLine(x);
ConsoleApp.Run(args, (string name, [Argument] int age = 20, bool force = false) => { });
""", "--help");

        await Assert.That(Stdout).IsEqualTo(expanded.Stdout);
        await Assert.That(ExitCode).IsEqualTo(expanded.ExitCode);
    }

    [Test]
    public async Task DirectMethodReference()
    {
        await verifier.Execute("""
ConsoleApp.Run(args, Run);

void Run([AsParameters] Options options)
{
    Console.Write($"{options.Value}:{options.Tag}");
}

public record class Options(int Value, string Tag = "x");
""", "--value 7", "7:x");
    }

    [Test]
    public async Task BuilderAddDelegateMethodReference()
    {
        await verifier.Execute("""
var app = ConsoleApp.Create();
app.Add("go", Go);
app.Run(args);

void Go([AsParameters] Options options)
{
    for (var i = 0; i < options.Times; i++)
    {
        Console.Write(options.Name);
    }
}

public record class Options(string Name, int Times = 1);
""", "go --name hi --times 3", "hihihi");
    }

    [Test]
    public async Task BuilderAddClassRegistration()
    {
        await verifier.Execute("""
var app = ConsoleApp.Create();
app.Add<Commands>();
app.Run(args);

public class Commands
{
    public void Do([AsParameters] Options options)
    {
        Console.Write($"{options.First}:{options.Second}");
    }
}

public record class Options(string First, [Argument] int Second);
""", "do --first A 9", "A:9");
    }

    [Test]
    public async Task MixedWithRegularParameter()
    {
        await verifier.Execute("""
ConsoleApp.Run(args, ([AsParameters] Options options, int repeat) =>
{
    for (var i = 0; i < repeat; i++)
    {
        Console.Write(options.Name);
    }
});

public record class Options(string Name);
""", "--name ab --repeat 3", "ababab");
    }

    [Test]
    public async Task MixedWithArgumentOrdering()
    {
        await verifier.Execute("""
ConsoleApp.Run(args, ([AsParameters] Options options, [Argument] int tail, int value) =>
{
    Console.Write($"{options.Head}:{tail}:{value}");
});

public record class Options([Argument] int Head);
""", "10 20 --value 30", "10:20:30");
    }

    [Test]
    public async Task MixedWithCancellationToken()
    {
        await verifier.Execute("""
ConsoleApp.Run(args, ([AsParameters] Options options, CancellationToken cancellationToken) =>
{
    Console.Write($"{options.Name}:{cancellationToken.IsCancellationRequested}");
});

public record class Options(string Name);
""", "--name test", "test:False");
    }

    [Test]
    public async Task MixedWithConsoleAppContext()
    {
        await verifier.Execute("""
var app = ConsoleApp.Create();
app.Add("go", ([AsParameters] Options options, ConsoleAppContext context) =>
{
    Console.Write($"{context.CommandName}:{options.Name}");
});
app.Run(args);

public record class Options(string Name);
""", "go --name abc", "go:abc");
    }

    [Test]
    public async Task MixedWithGlobalOptions()
    {
        await verifier.Execute("""
var app = ConsoleApp.Create();
app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    var env = builder.AddGlobalOption<string>("--env", "", "dev");
    return env;
});
app.Add("", ([AsParameters] Options options, ConsoleAppContext context) =>
{
    Console.Write($"{context.GlobalOptions}:{options.Name}");
});
app.Run(args);

public record class Options(string Name);
""", "--name neo --env prod", "prod:neo");
    }

    [Test]
    public async Task MixedWithRegularFromServices()
    {
        await verifier.Execute("""
var di = new MiniDI();
di.Register(typeof(Service), new Service("svc"));
ConsoleApp.ServiceProvider = di;

ConsoleApp.Run(args, ([AsParameters] Options options, [FromServices] Service service) =>
{
    Console.Write($"{service.Name}:{options.Name}");
});

public record class Options(string Name);

public class Service(string name)
{
    public string Name => name;
}

class MiniDI : IServiceProvider
{
    System.Collections.Generic.Dictionary<Type, object> dict = new();

    public void Register(Type type, object instance)
    {
        dict[type] = instance;
    }

    public object? GetService(Type serviceType)
    {
        return dict.TryGetValue(serviceType, out var instance) ? instance : null;
    }
}
""", "--name mixed", "svc:mixed");
    }

    [Test]
    public async Task MixedWithMultipleAsParameters()
    {
        await verifier.Execute("""
ConsoleApp.Run(args, ([AsParameters] UserOptions user, int level, [AsParameters] ModeOptions mode) =>
{
    Console.Write($"{user.Name}:{level}:{mode.Enabled}");
});

public record class UserOptions(string Name);
public record class ModeOptions(bool Enabled = false);
""", "--name Bob --level 9 --enabled", "Bob:9:True");
    }

    [Test]
    public async Task ConstructorXmlParamAliasesAndDescriptions()
    {
        var code = """
ConsoleApp.Log = x => Console.WriteLine(x);
ConsoleApp.Run(args, ([AsParameters] Options options) =>
{
    Console.Write($"{options.Name}:{options.Age}");
});

/// <summary>
/// Options for command.
/// </summary>
/// <param name="Name">-n, Name from doc.</param>
/// <param name="Age">-a, Age from doc.</param>
public record class Options(string Name, int Age);
""";

        await verifier.Execute(code, "-n Bob -a 21", "Bob:21");

        var (stdout, exitCode) = verifier.Error(code, "--help");
        await Assert.That(stdout).Contains("-n, --name <string>");
        await Assert.That(stdout).Contains("Name from doc.");
        await Assert.That(stdout).Contains("-a, --age <int>");
        await Assert.That(stdout).Contains("Age from doc.");
        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task ConstructorFromKeyedServices()
    {
        await verifier.Execute("""
var di = new MiniDI();
di.Register(typeof(MyService), "svc-key", new MyService("svc"));
ConsoleApp.ServiceProvider = di;

ConsoleApp.Run(args, ([AsParameters] Options options) =>
{
    Console.Write($"{options.Service.Name}:{options.Count}");
});

public record class Options(
    [FromKeyedServices("svc-key")] MyService Service,
    int Count);

public class MyService(string name)
{
    public string Name => name;
}

namespace Microsoft.Extensions.DependencyInjection
{
    public interface IKeyedServiceProvider : IServiceProvider
    {
        object? GetKeyedService(Type serviceType, object? serviceKey);
    }
}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class FromKeyedServicesAttribute(object? key) : Attribute
{
    public object? Key { get; } = key;
}

class MiniDI : Microsoft.Extensions.DependencyInjection.IKeyedServiceProvider
{
    System.Collections.Generic.Dictionary<(Type Type, object? Key), object> dict = new();

    public void Register(Type type, object? key, object instance)
    {
        dict[(type, key)] = instance;
    }

    public object? GetService(Type serviceType)
    {
        return null;
    }

    public object? GetKeyedService(Type serviceType, object? serviceKey)
    {
        return dict.TryGetValue((serviceType, serviceKey), out var instance) ? instance : null;
    }
}
""", "--count 9", "svc:9");
    }

    [Test]
    public async Task ConstructorHiddenAndHideDefaultValue()
    {
        var (stdout, exitCode) = verifier.Error("""
ConsoleApp.Log = x => Console.WriteLine(x);
ConsoleApp.Run(args, ([AsParameters] Options options) => { });

public record class Options(
    [Hidden] int Secret,
    [HideDefaultValue] int Level = 10);
""", "--help");

        await Assert.That(stdout.Contains("--secret")).IsFalse();
        await Assert.That(stdout).Contains("--level <int>");
        await Assert.That(stdout.Contains("[Default: 10]")).IsFalse();
        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task ConstructorCustomParser()
    {
        await verifier.Execute("""
ConsoleApp.Run(args, ([AsParameters] Options options) =>
{
    Console.Write(options.Value);
});

public record class Options([HexIntParser] int Value);

[AttributeUsage(AttributeTargets.Parameter)]
public class HexIntParserAttribute : Attribute, IArgumentParser<int>
{
    public static bool TryParse(ReadOnlySpan<char> s, out int result)
    {
        return int.TryParse(s, global::System.Globalization.NumberStyles.HexNumber, null, out result);
    }
}
""", "--value ff", "255");
    }
}
