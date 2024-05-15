using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConsoleAppFramework;

[Generator(LanguageNames.CSharp)]
public partial class ConsoleAppGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(EmitConsoleAppTemplateSource);

        var source = context.SyntaxProvider.CreateSyntaxProvider((node, ct) =>
        {
            if (node.IsKind(SyntaxKind.InvocationExpression))
            {
                var invocationExpression = (node as InvocationExpressionSyntax);
                if (invocationExpression == null) return false;

                var expr = invocationExpression.Expression as MemberAccessExpressionSyntax;
                if ((expr?.Expression as IdentifierNameSyntax)?.Identifier.Text == "ConsoleApp")
                {
                    var methodName = expr?.Name.Identifier.Text;
                    if (methodName is "Run" or "RunAsync")
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }, (context, ct) => ((InvocationExpressionSyntax)context.Node, context.SemanticModel));

        context.RegisterSourceOutput(source, EmitConsoleAppRun);
    }

    static void EmitConsoleAppTemplateSource(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("ConsoleApp.cs", """
namespace ConsoleAppFramework;

using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

internal interface IArgumentParser<T>
{
    static abstract bool TryParse(ReadOnlySpan<char> s, out T result);
}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class FromServicesAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class ArgumentAttribute : Attribute
{
}

internal static partial class ConsoleApp
{
    public static IServiceProvider? ServiceProvider { get; set; }
    public static TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    static Action<string>? logAction;
    public static Action<string> Log
    {
        get => logAction ??= Console.WriteLine;
        set => logAction = value;
    }

    static Action<string>? logErrorAction;
    public static Action<string> LogError
    {
        get => logErrorAction ??= Console.WriteLine;
        set => logErrorAction = value;
    }

    public static void Run(string[] args)
    {
    }

    public static Task RunAsync(string[] args)
    {
        return Task.CompletedTask;
    }

    static void ThrowArgumentParseFailed(string argumentName, string value)
    {
        throw new ArgumentException($"Argument '{argumentName}' parse failed. value: {value}");
    }

    static void ThrowRequiredArgumentNotParsed(string name)
    {
        throw new ArgumentException($"Require argument '{name}' does not parsed.");
    }

    static void ThrowArgumentNameNotFound(string argumentName)
    {
        throw new ArgumentException($"Argument '{argumentName}' does not found in command prameters.");
    }

    static bool TrySplitParse<T>(ReadOnlySpan<char> s, out T[] result)
       where T : ISpanParsable<T>
    {
        if (s.StartsWith("["))
        {
            try
            {
                result = System.Text.Json.JsonSerializer.Deserialize<T[]>(s)!;
            }
            catch
            {
                result = default!;
                return false;
            }
        }

        var count = s.Count(',') + 1;
        result = new T[count];

        var source = s;
        var destination = result.AsSpan();
        Span<Range> ranges = stackalloc Range[Math.Min(count, 128)];

        while (true)
        {
            var splitCount = source.Split(ranges, ',');
            var parseTo = splitCount;
            if (splitCount == 128 && source[ranges[^1]].Contains(','))
            {
                parseTo = splitCount - 1;
            }

            for (int i = 0; i < parseTo; i++)
            {
                if (!T.TryParse(source[ranges[i]], null, out destination[i]!))
                {
                    return false;
                }
            }
            destination = destination.Slice(parseTo);

            if (destination.Length != 0)
            {
                source = source[ranges[^1]];
                continue;
            }
            else
            {
                break;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryShowHelpOrVersion(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp(); // TODO: if no args root command, return false.
            return true;
        }

        if (args.Length == 1)
        {
            switch (args[0])
            {
                case "--version":
                    ShowVersion();
                    return true;
                case "-h":
                case "--help":
                    ShowHelp();
                    return true;
                default:
                    break;
            }
        }

        return false;
    }

    static void ShowVersion()
    {
        var asm = Assembly.GetEntryAssembly();
        var version = "1.0.0";
        var infoVersion = asm!.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (infoVersion != null)
        {
            version = infoVersion.InformationalVersion;
        }
        else
        {
            var asmVersion = asm!.GetCustomAttribute<AssemblyVersionAttribute>();
            if (asmVersion != null)
            {
                version = asmVersion.Version;
            }
        }
        Log(version);
    }

    static void ShowHelp()
    {
        Log("TODO: Build Help");
    }

    sealed class PosixSignalHandler : IDisposable
    {
        public CancellationToken Token => cancellationTokenSource.Token;
        public CancellationToken TimeoutToken => timeoutCancellationTokenSource.Token;

        CancellationTokenSource cancellationTokenSource;
        CancellationTokenSource timeoutCancellationTokenSource;
        TimeSpan timeout;

        PosixSignalRegistration? sigInt;
        PosixSignalRegistration? sigQuit;
        PosixSignalRegistration? sigTerm;

        PosixSignalHandler(TimeSpan timeout)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.timeoutCancellationTokenSource = new CancellationTokenSource();
            this.timeout = timeout;
        }

        public static PosixSignalHandler Register(TimeSpan timeout)
        {
            var handler = new PosixSignalHandler(timeout);

            Action<PosixSignalContext> handleSignal = handler.HandlePosixSignal;

            handler.sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, handleSignal);
            handler.sigQuit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, handleSignal);
            handler.sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, handleSignal);

            return handler;
        }

        void HandlePosixSignal(PosixSignalContext context)
        {
            context.Cancel = true;
            cancellationTokenSource.Cancel();
            timeoutCancellationTokenSource.CancelAfter(timeout);
        }

        public void Dispose()
        {
            sigInt?.Dispose();
            sigQuit?.Dispose();
            sigTerm?.Dispose();
            timeoutCancellationTokenSource.Dispose();
        }
    }
}
""");
    }

    static void EmitConsoleAppRun(SourceProductionContext sourceProductionContext, (InvocationExpressionSyntax, SemanticModel) generatorSyntaxContext)
    {
        var node = generatorSyntaxContext.Item1;
        var model = generatorSyntaxContext.Item2;

        var wellKnownTypes = new WellKnownTypes(model.Compilation);

        var parser = new Parser(sourceProductionContext, node, model, wellKnownTypes);
        var command = parser.ParseAndValidate();
        if (command == null)
        {
            return;
        }

        var emitter = new Emitter(command, wellKnownTypes);

        var isRunAsync = ((node.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.Text == "RunAsync");
        var code = emitter.EmitRun(isRunAsync);

        sourceProductionContext.AddSource("ConsoleApp.Run.cs", $$"""
// <auto-generated/>
#nullable enable
#pragma warning disable CS0108 // hides inherited member
#pragma warning disable CS0162 // Unreachable code
#pragma warning disable CS0164 // This label has not been referenced
#pragma warning disable CS0219 // Variable assigned but never used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8601 // Possible null reference assignment
#pragma warning disable CS8602
#pragma warning disable CS8604 // Possible null reference argument for parameter
#pragma warning disable CS8619
#pragma warning disable CS8620
#pragma warning disable CS8631 // The type cannot be used as type parameter in the generic type or method
#pragma warning disable CS8765 // Nullability of type of parameter
#pragma warning disable CS9074 // The 'scoped' modifier of parameter doesn't match overridden or implemented member
#pragma warning disable CA1050 // Declare types in namespaces.

namespace ConsoleAppFramework;

using System;
using System.Threading.Tasks;

internal static partial class ConsoleApp
{
{{code}}
}
""");
    }
}