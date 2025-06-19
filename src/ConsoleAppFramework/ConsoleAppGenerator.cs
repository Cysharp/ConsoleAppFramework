using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Reflection;

namespace ConsoleAppFramework;

[Generator(LanguageNames.CSharp)]
public partial class ConsoleAppGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Emit ConsoleApp.g.cs
        context.RegisterPostInitializationOutput(EmitConsoleAppTemplateSource);

        // Emit ConfigureConfiguration/Logging/Services and Host.AsConsoleApp
        var hasReferences = context.MetadataReferencesProvider
            .Collect()
            .Select((xs, _) =>
            {
                var hasDependencyInjection = false;
                var hasLogging = false;
                var hasConfiguration = false;
                var hasJsonConfiguration = false;
                var hasHost = false;

                foreach (var x in xs)
                {
                    var name = x.Display;
                    if (name == null) continue;

                    if (!hasDependencyInjection && name.EndsWith("Microsoft.Extensions.DependencyInjection.dll")) // BuildServiceProvider, IKeyedServiceProvider
                    {
                        hasDependencyInjection = true;
                        continue;
                    }

                    if (!hasLogging && name.EndsWith("Microsoft.Extensions.Logging.dll")) // AddLogging
                    {
                        hasLogging = true;
                        continue;
                    }

                    if (!hasConfiguration && name.EndsWith("Microsoft.Extensions.Configuration.dll")) // needs ConfigurationBuilder
                    {
                        hasConfiguration = true;
                        continue;
                    }

                    if (!hasJsonConfiguration && name.EndsWith("Microsoft.Extensions.Configuration.Json.dll")) // AddJson
                    {
                        hasJsonConfiguration = true;
                        continue;
                    }

                    if (!hasHost && name.EndsWith("Microsoft.Extensions.Hosting.dll")) // IHostBuilder, ApplicationHostBuilder
                    {
                        hasHost = true;
                        continue;
                    }
                }

                return new DllReference(hasDependencyInjection, hasLogging, hasConfiguration, hasJsonConfiguration, hasHost);
            });

        context.RegisterSourceOutput(hasReferences, EmitConsoleAppConfigure);

        // get Options for Combine
        var generatorOptions = context.CompilationProvider.Select((compilation, token) =>
        {
            foreach (var attr in compilation.Assembly.GetAttributes())
            {
                if (attr.AttributeClass?.Name == "ConsoleAppFrameworkGeneratorOptionsAttribute")
                {
                    var args = attr.NamedArguments;
                    var disableNamingConversion = args.FirstOrDefault(x => x.Key == "DisableNamingConversion").Value.Value as bool? ?? false;
                    return new ConsoleAppFrameworkGeneratorOptions(disableNamingConversion);
                }
            }

            return new ConsoleAppFrameworkGeneratorOptions(DisableNamingConversion: false);
        });

        // ConsoleApp.Run
        var runSource = context.SyntaxProvider
            .CreateSyntaxProvider((node, ct) =>
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
            }, (context, ct) => context)
            .Combine(generatorOptions)
            .Select((t, ct) =>
            {
                var (context, options) = t;
                var reporter = new DiagnosticReporter();
                var node = (InvocationExpressionSyntax)context.Node;
                var wellknownTypes = new WellKnownTypes(context.SemanticModel.Compilation);
                var parser = new Parser(options, reporter, node, context.SemanticModel, wellknownTypes, DelegateBuildType.MakeCustomDelegateWhenHasDefaultValueOrTooLarge, []);
                var isRunAsync = (node.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.Text == "RunAsync";

                var command = parser.ParseAndValidateForRun();
                return new CommandContext(command, isRunAsync, reporter, node);
            })
            .WithTrackingName("ConsoleApp.Run.0_CreateSyntaxProvider"); // annotate for IncrementalGeneratorTest

        context.RegisterSourceOutput(runSource, EmitConsoleAppRun);

        // ConsoleAppBuilder
        var builderSource = context.SyntaxProvider
            .CreateSyntaxProvider((node, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                if (node.IsKind(SyntaxKind.InvocationExpression))
                {
                    var invocationExpression = (node as InvocationExpressionSyntax);
                    if (invocationExpression == null) return false;

                    var expr = invocationExpression.Expression as MemberAccessExpressionSyntax;
                    var methodName = expr?.Name.Identifier.Text;
                    if (methodName is "Add" or "UseFilter" or "Run" or "RunAsync")
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }, (context, ct) => new BuilderContext( // no equality check
                (InvocationExpressionSyntax)context.Node,
                ((context.Node as InvocationExpressionSyntax)!.Expression as MemberAccessExpressionSyntax)!.Name.Identifier.Text,
                context.SemanticModel,
                ct))
            .WithTrackingName("ConsoleApp.Builder.0_CreateSyntaxProvider")
            .Where(x =>
            {
                var model = x.Model.GetTypeInfo((x.Node.Expression as MemberAccessExpressionSyntax)!.Expression, x.CancellationToken);
                return model.Type?.Name is "ConsoleAppBuilder" or "IHostBuilder" || model.Type?.Kind == SymbolKind.ErrorType; // allow ErrorType(ConsoleAppBuilder from Configure***(Source Generator generated method) is unknown in Source Generator)
            })
            .WithTrackingName("ConsoleApp.Builder.1_Where")
            .Collect()
            .Combine(generatorOptions)
            .Select((x, ct) => new CollectBuilderContext(x.Right, x.Left, ct))
            .WithTrackingName("ConsoleApp.Builder.2_Collect");

        var registerCommands = context.SyntaxProvider.ForAttributeWithMetadataName("ConsoleAppFramework.RegisterCommandsAttribute",
            (node, token) => true,
            (ctx, token) => ctx)
            .Collect();

        var combined = builderSource.Combine(registerCommands)
            .WithTrackingName("ConsoleApp.Builder.3_Combined")
            .Select((tuple, token) =>
            {
                var (context, commands) = tuple;
                context.AddRegisterAttributes(commands);
                return context;
            })
            .WithTrackingName("ConsoleApp.Builder.4_CombineSelected");

        context.RegisterSourceOutput(combined, EmitConsoleAppBuilder);
    }

    static void EmitConsoleAppTemplateSource(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("ConsoleApp.g.cs", ConsoleAppBaseCode.InitializationCode.ReplaceLineEndings());
    }

    static void EmitConsoleAppRun(SourceProductionContext sourceProductionContext, CommandContext commandContext)
    {
        if (commandContext.DiagnosticReporter.HasDiagnostics)
        {
            commandContext.DiagnosticReporter.ReportToContext(sourceProductionContext);
            return;
        }
        var command = commandContext.Command;
        if (command == null) return;

        if (command.HasFilter)
        {
            sourceProductionContext.ReportDiagnostic(DiagnosticDescriptors.CommandHasFilter, commandContext.Node.GetLocation());
            return;
        }

        var sb = new SourceBuilder(0);
        sb.AppendLine(ConsoleAppBaseCode.GeneratedCodeHeader);
        using (sb.BeginBlock("internal static partial class ConsoleApp"))
        {
            var emitter = new Emitter();
            var withId = new Emitter.CommandWithId(null, command, -1);
            emitter.EmitRun(sb, withId, command.IsAsync);
        }
        sourceProductionContext.AddSource("ConsoleApp.Run.g.cs", sb.ToString().ReplaceLineEndings());

        var help = new SourceBuilder(0);
        help.AppendLine(ConsoleAppBaseCode.GeneratedCodeHeader);
        using (help.BeginBlock("internal static partial class ConsoleApp"))
        {
            var emitter = new Emitter();
            emitter.EmitHelp(help, command);
        }
        sourceProductionContext.AddSource("ConsoleApp.Run.Help.g.cs", help.ToString().ReplaceLineEndings());
    }

    static void EmitConsoleAppBuilder(SourceProductionContext sourceProductionContext, CollectBuilderContext collectBuilderContext)
    {
        var reporter = collectBuilderContext.DiagnosticReporter;
        var hasRun = collectBuilderContext.HasRun;
        var hasRunAsync = collectBuilderContext.HasRunAsync;

        if (reporter.HasDiagnostics)
        {
            reporter.ReportToContext(sourceProductionContext);
            return;
        }

        if (!hasRun && !hasRunAsync) return;

        var sb = new SourceBuilder(0);
        sb.AppendLine(ConsoleAppBaseCode.GeneratedCodeHeader);

        var delegateSignatures = new List<string>();

        // with id number
        var commandIds = collectBuilderContext.Commands
            .Select((x, i) =>
            {
                var command = new Emitter.CommandWithId(
                    FieldType: x!.BuildDelegateSignature(Emitter.CommandWithId.BuildCustomDelegateTypeName(i), out var delegateDef),
                    Command: x!,
                    Id: i
                );
                if (delegateDef != null)
                {
                    delegateSignatures.Add(delegateDef);
                }
                return command;
            })
            .ToArray();

        using (sb.BeginBlock("internal static partial class ConsoleApp"))
        {
            foreach (var d in delegateSignatures)
            {
                sb.AppendLine(d);
            }

            var emitter = new Emitter();
            emitter.EmitBuilder(sb, commandIds, hasRun, hasRunAsync);
        }
        sourceProductionContext.AddSource("ConsoleApp.Builder.g.cs", sb.ToString().ReplaceLineEndings());

        // Build Help

        var help = new SourceBuilder(0);
        help.AppendLine(ConsoleAppBaseCode.GeneratedCodeHeader);
        using (help.BeginBlock("internal static partial class ConsoleApp"))
        using (help.BeginBlock("internal partial class ConsoleAppBuilder"))
        {
            var emitter = new Emitter();
            emitter.EmitHelp(help, commandIds!);
        }
        sourceProductionContext.AddSource("ConsoleApp.Builder.Help.g.cs", help.ToString().ReplaceLineEndings());
    }

    static void EmitConsoleAppConfigure(SourceProductionContext sourceProductionContext, DllReference dllReference)
    {
        if (!dllReference.HasDependencyInjection && !dllReference.HasLogging && !dllReference.HasConfiguration && !dllReference.HasHost)
        {
            return;
        }

        var sb = new SourceBuilder(0);
        sb.AppendLine(ConsoleAppBaseCode.GeneratedCodeHeader);

        if (dllReference.HasDependencyInjection)
        {
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        }
        if (dllReference.HasLogging)
        {
            sb.AppendLine("using Microsoft.Extensions.Logging;");
        }
        if (dllReference.HasConfiguration || dllReference.HasJsonConfiguration)
        {
            sb.AppendLine("using Microsoft.Extensions.Configuration;");
        }

        if (dllReference.HasHost)
        {
            var sb2 = sb.Clone();
            sb2.AppendLine("using Microsoft.Extensions.Hosting;");
            var emitter = new Emitter();
            emitter.EmitAsConsoleAppBuilder(sb2, dllReference);
            sourceProductionContext.AddSource("ConsoleAppHostBuilderExtensions.g.cs", sb2.ToString().ReplaceLineEndings());
        }

        using (sb.BeginBlock("internal static partial class ConsoleApp"))
        using (sb.BeginBlock("internal partial class ConsoleAppBuilder"))
        {
            var emitter = new Emitter();
            emitter.EmitConfigure(sb, dllReference);
        }

        sourceProductionContext.AddSource("ConsoleApp.Builder.Configure.g.cs", sb.ToString().ReplaceLineEndings());
    }

    class CommandContext(Command? command, bool isAsync, DiagnosticReporter diagnosticReporter, InvocationExpressionSyntax node) : IEquatable<CommandContext>
    {
        public Command? Command => command;
        public DiagnosticReporter DiagnosticReporter => diagnosticReporter;
        public InvocationExpressionSyntax Node => node;
        public bool IsAsync => isAsync;

        public bool Equals(CommandContext other)
        {
            // has diagnostics, always go to modified(don't cache)
            if (diagnosticReporter.HasDiagnostics || other.DiagnosticReporter.HasDiagnostics) return false;
            if (command == null || other.Command == null) return false; // maybe has diagnostics

            if (isAsync != other.IsAsync) return false;
            return command.Equals(other.Command);
        }
    }

    class CollectBuilderContext : IEquatable<CollectBuilderContext>
    {
        public Command[] Commands { get; private set; } = [];
        public DiagnosticReporter DiagnosticReporter { get; }
        public CancellationToken CancellationToken { get; }
        public bool HasRun { get; }
        public bool HasRunAsync { get; }

        FilterInfo[]? globalFilters { get; }
        ConsoleAppFrameworkGeneratorOptions generatorOptions { get; }

        public CollectBuilderContext(ConsoleAppFrameworkGeneratorOptions generatorOptions, ImmutableArray<BuilderContext> contexts, CancellationToken cancellationToken)
        {
            this.DiagnosticReporter = new DiagnosticReporter();
            this.CancellationToken = cancellationToken;
            this.generatorOptions = generatorOptions;

            // validation, invoke in loop is not allowed.
            foreach (var item in contexts)
            {
                if (item.Name is "Run" or "RunAsync") continue;
                foreach (var n in item.Node.Ancestors())
                {
                    if (n.Kind() is SyntaxKind.WhileStatement or SyntaxKind.DoStatement or SyntaxKind.ForStatement or SyntaxKind.ForEachStatement)
                    {
                        DiagnosticReporter.ReportDiagnostic(DiagnosticDescriptors.AddInLoopIsNotAllowed, item.Node.GetLocation());
                        return;
                    }
                }
            }

            var methodGroup = contexts.ToLookup(x =>
            {
                if (x.Name == "Add" && ((x.Node.Expression as MemberAccessExpressionSyntax)?.Name.IsKind(SyntaxKind.GenericName) ?? false))
                {
                    return "Add<T>";
                }

                return x.Name;
            });

            globalFilters = methodGroup["UseFilter"]
                .OrderBy(x => x.Node.GetLocation().SourceSpan) // sort by line number
                .Select(x =>
                {
                    var genericName = (x.Node.Expression as MemberAccessExpressionSyntax)?.Name as GenericNameSyntax;
                    var genericType = genericName!.TypeArgumentList.Arguments[0];
                    var type = x.Model.GetTypeInfo(genericType).Type;
                    if (type == null) return null!;

                    var filter = FilterInfo.Create(type);

                    if (filter == null)
                    {
                        DiagnosticReporter.ReportDiagnostic(DiagnosticDescriptors.FilterMultipleConstructor, genericType.GetLocation());
                        return null!;
                    }

                    return filter!;
                })
                .ToArray();

            // don't emit if exists failure
            if (DiagnosticReporter.HasDiagnostics)
            {
                globalFilters = null;
                return;
            }

            var names = new HashSet<string>();
            var commands1 = methodGroup["Add"]
                .Select(x =>
                {
                    var wellKnownTypes = new WellKnownTypes(x.Model.Compilation);
                    var parser = new Parser(generatorOptions, DiagnosticReporter, x.Node, x.Model, wellKnownTypes, DelegateBuildType.MakeCustomDelegateWhenHasDefaultValueOrTooLarge, globalFilters);
                    var command = parser.ParseAndValidateForBuilderDelegateRegistration();

                    // validation command name duplicate
                    if (command != null && !names.Add(command.Name))
                    {
                        var location = x.Node.ArgumentList.Arguments[0].GetLocation();
                        DiagnosticReporter.ReportDiagnostic(DiagnosticDescriptors.DuplicateCommandName, location, command!.Name);
                        return null;
                    }

                    return command;
                })
                .ToArray(); // evaluate first.

            var commands2 = methodGroup["Add<T>"]
                .SelectMany(x =>
                {
                    var wellKnownTypes = new WellKnownTypes(x.Model.Compilation);
                    var parser = new Parser(generatorOptions, DiagnosticReporter, x.Node, x.Model, wellKnownTypes, DelegateBuildType.None, globalFilters);
                    var commands = parser.ParseAndValidateForBuilderClassRegistration();

                    // validation command name duplicate
                    foreach (var command in commands)
                    {
                        if (command != null && !names.Add(command.Name))
                        {
                            DiagnosticReporter.ReportDiagnostic(DiagnosticDescriptors.DuplicateCommandName, x.Node.GetLocation(), command!.Name);
                            return [null];
                        }
                    }

                    return commands;
                });

            if (DiagnosticReporter.HasDiagnostics)
            {
                return;
            }

            // set properties
            this.Commands = commands1.Concat(commands2!).Where(x => x != null).ToArray()!;
            this.HasRun = methodGroup["Run"].Any();
            this.HasRunAsync = methodGroup["RunAsync"].Any();
        }

        // from ForAttributeWithMetadataName
        public void AddRegisterAttributes(ImmutableArray<GeneratorAttributeSyntaxContext> contexts)
        {
            if (contexts.Length == 0 || DiagnosticReporter.HasDiagnostics)
            {
                return;
            }

            var names = new HashSet<string>(Commands.Select(x => x.Name));

            var list = new List<Command>();
            foreach (var ctx in contexts)
            {
                string? commandPath = null;
                var attrData = ctx.Attributes[0]; // AllowMultiple = false
                if (attrData.ConstructorArguments.Length != 0)
                {
                    commandPath = attrData.ConstructorArguments[0].Value as string;
                }

                var wellKnownTypes = new WellKnownTypes(ctx.SemanticModel.Compilation);
                var parser = new Parser(generatorOptions, DiagnosticReporter, ctx.TargetNode, ctx.SemanticModel, wellKnownTypes, DelegateBuildType.None, globalFilters ?? []);

                var commands = parser.CreateCommandsFromType((ITypeSymbol)ctx.TargetSymbol, commandPath);

                foreach (var command in commands)
                {
                    if (command != null)
                    {
                        if (!names.Add(command.Name))
                        {
                            var methodSymbol = command.Symbol.Value as IMethodSymbol;
                            var location = methodSymbol?.Locations[0] ?? ctx.TargetNode.GetLocation();
                            DiagnosticReporter.ReportDiagnostic(DiagnosticDescriptors.DuplicateCommandName, location, command!.Name);
                            break;
                        }
                        else
                        {
                            list.Add(command);
                        }
                    }
                }
            }

            Commands = Commands.Concat(list).Where(x => x != null).ToArray();
        }

        public bool Equals(CollectBuilderContext other)
        {
            if (DiagnosticReporter.HasDiagnostics || other.DiagnosticReporter.HasDiagnostics) return false;
            if (HasRun != other.HasRun) return false;
            if (HasRunAsync != other.HasRunAsync) return false;

            return Commands.AsSpan().SequenceEqual(other.Commands);
        }
    }

    // intermediate structure(no equatable)
    readonly struct BuilderContext(InvocationExpressionSyntax node, string name, SemanticModel model, CancellationToken cancellationToken) : IEquatable<BuilderContext>
    {
        public InvocationExpressionSyntax Node => node;
        public string Name => name;
        public SemanticModel Model => model;
        public CancellationToken CancellationToken => cancellationToken;

        public bool Equals(BuilderContext other)
        {
            return Node == other.Node; // no means.
        }
    }
}
