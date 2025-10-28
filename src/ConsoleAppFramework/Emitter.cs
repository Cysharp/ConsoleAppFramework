﻿using Microsoft.CodeAnalysis;
using System.Reflection.Metadata;

namespace ConsoleAppFramework;

internal class Emitter(DllReference? dllReference) // from EmitConsoleAppRun, null.
{
    public void EmitRun(SourceBuilder sb, CommandWithId commandWithId, bool isRunAsync, string? methodName)
    {
        var command = commandWithId.Command;

        var emitForBuilder = methodName != null;
        var hasCancellationToken = command.Parameters.Any(x => x.IsCancellationToken);
        var hasConsoleAppContext = command.Parameters.Any(x => x.IsConsoleAppContext);
        var hasArgument = command.Parameters.Any(x => x.IsArgument);
        var hasValidation = command.Parameters.Any(x => x.HasValidation);
        var parsableParameterCount = command.Parameters.Count(x => x.IsParsable);
        var requiredParsableParameterCount = command.Parameters.Count(x => x.IsParsable && x.RequireCheckArgumentParsed);

        if (command.HasFilter)
        {
            isRunAsync = true;
            hasCancellationToken = false;
            hasConsoleAppContext = false;
        }
        var returnType = isRunAsync ? "async Task" : "void";
        var accessibility = !emitForBuilder ? "public static" : "private";
        methodName = methodName ?? (isRunAsync ? "RunAsync" : "Run");
        var unsafeCode = (command.MethodKind == MethodKind.FunctionPointer) ? "unsafe " : "";

        var commandMethodType = command.BuildDelegateSignature(commandWithId.BuildCustomDelegateTypeName(), out var delegateType);
        if (commandMethodType != null)
        {
            commandMethodType = $", {commandMethodType} command";
        }

        // emit custom delegate type
        if (delegateType != null && !emitForBuilder)
        {
            sb.AppendLine($"internal {delegateType}");
            sb.AppendLine();
        }

        var commandDepthEscapeIndex = emitForBuilder ? ", int commandDepth, int escapeIndex" : "";
        var filterCancellationToken = command.HasFilter ? ", ConsoleAppContext context, CancellationToken cancellationToken"
                                    : emitForBuilder ? ", CancellationToken __ExternalCancellationToken__"
                                    : "";
        var cancellationTokenName = command.HasFilter ? "cancellationToken"
                                    : emitForBuilder ? "__ExternalCancellationToken__"
                                    : "CancellationToken.None";

        if (!emitForBuilder)
        {
            sb.AppendLine("/// <summary>");
            var help = CommandHelpBuilder.BuildCommandHelpMessage(commandWithId.Command);
#pragma warning disable RS1035
            foreach (var line in help.Split([Environment.NewLine], StringSplitOptions.None))
            {
                sb.AppendLine($"/// {line.Replace("<", "&lt;").Replace(">", "&gt;")}<br/>");
            }
#pragma warning restore RS1035
            sb.AppendLine("/// </summary>");
        }

        // method signature
        using (sb.BeginBlock($"{accessibility} {unsafeCode}{returnType} {methodName}(string[] args{commandDepthEscapeIndex}{commandMethodType}{filterCancellationToken})"))
        {
            using (command.HasFilter ? sb.Nop : sb.BeginBlock("try"))
            {
                if (emitForBuilder)
                {
                    sb.AppendLine("var commandArgs = (escapeIndex == -1) ? args.AsSpan(commandDepth) : args.AsSpan(commandDepth, escapeIndex - commandDepth);");
                }
                else
                {
                    if (hasConsoleAppContext)
                    {
                        sb.AppendLine("var escapeIndex = args.AsSpan().IndexOf(\"--\");");
                        sb.AppendLine("var commandArgs = (escapeIndex == -1) ? args.AsSpan() : args.AsSpan(0, escapeIndex);");
                    }
                    else
                    {
                        sb.AppendLine("var commandArgs = args.AsSpan();");
                    }
                }

                sb.AppendLine($"if (TryShowHelpOrVersion(commandArgs, {requiredParsableParameterCount}, {commandWithId.Id})) return;");
                sb.AppendLine();

                // prepare argument variables
                if (hasCancellationToken)
                {
                    sb.AppendLine($"using var posixSignalHandler = PosixSignalHandler.Register(Timeout, {cancellationTokenName});");
                    sb.AppendLine();
                    cancellationTokenName = "posixSignalHandler.Token";
                }

                if (hasConsoleAppContext)
                {
                    if (emitForBuilder)
                    {
                        sb.AppendLine("ConsoleAppContext context;");
                        using (sb.BeginBlock("if (configureGlobalOptions == null)"))
                        {
                            sb.AppendLine($"context = new ConsoleAppContext(\"{command.Name}\", args, args, null, null, commandDepth, escapeIndex);");
                        }
                        using (sb.BeginBlock("else"))
                        {
                            sb.AppendLine("var builder = new GlobalOptionsBuilder(commandArgs);");
                            sb.AppendLine("var globalOptions = configureGlobalOptions(ref builder);");
                            sb.AppendLine($"context = new ConsoleAppContext(\"{command.Name}\", args, args, null, globalOptions, commandDepth, escapeIndex);");
                            sb.AppendLine("commandArgs = builder.RemainingArgs;");
                        }
                        sb.AppendLine("BuildAndSetServiceProvider(context);");

                        if (dllReference != null && dllReference.Value.HasHost)
                        {
                            sb.AppendLine("host = ConsoleApp.ServiceProvider?.GetService(typeof(Microsoft.Extensions.Hosting.IHost)) as Microsoft.Extensions.Hosting.IHost;");
                            using (sb.BeginBlock("if (startHost && host != null)"))
                            {
                                if (isRunAsync)
                                {
                                    sb.AppendLine($"await host.StartAsync({cancellationTokenName});");
                                }
                                else
                                {
                                    sb.AppendLine($"host.StartAsync({cancellationTokenName}).GetAwaiter().GetResult();");
                                }
                            }
                        }
                    }
                    else
                    {
                        sb.AppendLine($"var context = new ConsoleAppContext(\"{command.Name}\", args, args, null, null, 0, escapeIndex);");
                    }
                    sb.AppendLine();
                }

                for (var i = 0; i < command.Parameters.Length; i++)
                {
                    var parameter = command.Parameters[i];
                    if (parameter.IsParsable)
                    {
                        var defaultValue = parameter.IsParams ? $"({parameter.ToTypeDisplayString()})[]"
                                         : parameter.HasDefaultValue ? parameter.DefaultValueToString()
                                         : $"default({parameter.Type.ToFullyQualifiedFormatDisplayString()})";
                        sb.AppendLine($"var arg{i} = {defaultValue};");
                        if (parameter.RequireCheckArgumentParsed)
                        {
                            sb.AppendLine($"var arg{i}Parsed = false;");
                        }
                    }
                    else if (parameter.IsCancellationToken)
                    {
                        if (command.HasFilter)
                        {
                            sb.AppendLine($"var arg{i} = cancellationToken;");
                        }
                        else
                        {
                            sb.AppendLine($"var arg{i} = posixSignalHandler.Token;");
                        }
                    }
                    else if (parameter.IsConsoleAppContext)
                    {
                        sb.AppendLine($"var arg{i} = context;");
                    }
                    else if (parameter.IsFromServices)
                    {
                        var type = parameter.Type.ToFullyQualifiedFormatDisplayString();
                        sb.AppendLine($"var arg{i} = ({type})ServiceProvider!.GetService(typeof({type}))!;");
                    }
                    else if (parameter.IsFromKeyedServices)
                    {
                        var type = parameter.Type.ToFullyQualifiedFormatDisplayString();
                        var line = $"var arg{i} = ({type})((Microsoft.Extensions.DependencyInjection.IKeyedServiceProvider)ServiceProvider).GetKeyedService(typeof({type}), {parameter.GetFormattedKeyedServiceKey()})!;";
                        sb.AppendLine(line);
                    }
                }
                sb.AppendLineIfExists(command.Parameters.AsSpan());

                if (hasArgument)
                {
                    sb.AppendLine("var argumentPosition = 0;");
                }

                using (sb.BeginBlock("for (int i = 0; i < commandArgs.Length; i++)"))
                {
                    sb.AppendLine("var name = commandArgs[i];");
                    if (hasArgument)
                    {
                        sb.AppendLine("var optionCandidate = name.Length > 1 && name[0] == '-' && !char.IsDigit(name[1]);");
                    }
                    sb.AppendLine();

                    if (!command.Parameters.All(p => !p.IsParsable || p.IsArgument))
                    {
                        using (hasArgument ? sb.BeginBlock("if (optionCandidate)") : sb.Nop)
                        {
                            using (sb.BeginBlock("switch (name)"))
                            {
                                // parse argument(fast, switch directly)
                                for (int i = 0; i < command.Parameters.Length; i++)
                                {
                                    var parameter = command.Parameters[i];
                                    if (!parameter.IsParsable) continue;
                                    if (parameter.IsArgument) continue;

                                    sb.AppendLine($"case \"--{parameter.Name}\":");
                                    foreach (var alias in parameter.Aliases)
                                    {
                                        sb.AppendLine($"case \"{alias}\":");
                                    }
                                    using (sb.BeginBlock())
                                    {
                                        sb.AppendLine($"{parameter.BuildParseMethod(i, parameter.Name, increment: true)}");
                                        if (parameter.RequireCheckArgumentParsed)
                                        {
                                            sb.AppendLine($"arg{i}Parsed = true;");
                                        }
                                        sb.AppendLine("continue;");
                                    }
                                }

                                using (sb.BeginIndent("default:"))
                                {
                                    // parse argument(slow, ignorecase)
                                    for (int i = 0; i < command.Parameters.Length; i++)
                                    {
                                        var parameter = command.Parameters[i];
                                        if (!parameter.IsParsable) continue;
                                        if (parameter.IsArgument) continue;

                                        sb.AppendLine($"if (string.Equals(name, \"--{parameter.Name}\", StringComparison.OrdinalIgnoreCase){(parameter.Aliases.Length == 0 ? ")" : "")}");
                                        for (int j = 0; j < parameter.Aliases.Length; j++)
                                        {
                                            var alias = parameter.Aliases[j];
                                            sb.AppendLine($" || string.Equals(name, \"{alias}\", StringComparison.OrdinalIgnoreCase){(parameter.Aliases.Length == j + 1 ? ")" : "")}");
                                        }
                                        using (sb.BeginBlock())
                                        {
                                            sb.AppendLine($"{parameter.BuildParseMethod(i, parameter.Name, increment: true)}");
                                            if (parameter.RequireCheckArgumentParsed)
                                            {
                                                sb.AppendLine($"arg{i}Parsed = true;");
                                            }
                                            sb.AppendLine("continue;");
                                        }
                                    }

                                    sb.AppendLine("ThrowArgumentNameNotFound(name);");
                                    sb.AppendLine("break;");
                                }
                            }
                        }
                    }

                    // parse indexed argument([Argument] parameter)
                    if (hasArgument)
                    {
                        for (int i = 0; i < command.Parameters.Length; i++)
                        {
                            var parameter = command.Parameters[i];
                            if (!parameter.IsArgument) continue;

                            sb.AppendLine($"if (argumentPosition == {parameter.ArgumentIndex})");
                            using (sb.BeginBlock())
                            {
                                sb.AppendLine($"{parameter.BuildParseMethod(i, parameter.Name, increment: false)}");
                                if (parameter.RequireCheckArgumentParsed)
                                {
                                    sb.AppendLine($"arg{i}Parsed = true;");
                                }
                                sb.AppendLine("argumentPosition++;");
                                sb.AppendLine("continue;");
                            }
                        }
                        sb.AppendLine();
                    }

                    if (hasArgument)
                    {
                        sb.AppendLine("ThrowArgumentNameNotFound(name);");
                    }
                }

                // validate parsed
                for (int i = 0; i < command.Parameters.Length; i++)
                {
                    var parameter = command.Parameters[i];
                    if (!parameter.IsParsable) continue;

                    if (parameter.RequireCheckArgumentParsed)
                    {
                        sb.AppendLine($"if (!arg{i}Parsed) ThrowRequiredArgumentNotParsed(\"{parameter.Name}\");");
                    }
                }

                // attribute validation
                if (hasValidation)
                {
                    sb.AppendLine();
                    sb.AppendLine("var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(\"\", null, null);");
                    if (command.CommandMethodInfo == null)
                    {
                        sb.AppendLine("var parameters = command.Method.GetParameters();");
                    }
                    else
                    {
                        sb.AppendLine($"var parameters = typeof({command.CommandMethodInfo.TypeFullName}).GetMethod(\"{command.CommandMethodInfo.MethodName}\").GetParameters();");
                    }
                    sb.AppendLine("System.Text.StringBuilder? errorMessages = null;");
                    for (int i = 0; i < command.Parameters.Length; i++)
                    {
                        var parameter = command.Parameters[i];
                        if (!parameter.HasValidation) continue;

                        sb.AppendLine($"ValidateParameter(arg{i}, parameters[{i}], validationContext, ref errorMessages);");
                    }
                    sb.AppendLine("if (errorMessages != null)");
                    using (sb.BeginBlock())
                    {
                        sb.AppendLine("throw new System.ComponentModel.DataAnnotations.ValidationException(errorMessages.ToString());");
                    }
                }

                // invoke for sync/async, void/int
                sb.AppendLine();
                var methodArguments = string.Join(", ", command.Parameters.Select((x, i) => $"arg{i}!"));
                string invokeCommand;
                if (command.CommandMethodInfo == null)
                {
                    invokeCommand = $"command({methodArguments})";
                }
                else
                {
                    var usingInstance = (isRunAsync, command.CommandMethodInfo.IsIDisposable, command.CommandMethodInfo.IsIAsyncDisposable) switch
                    {
                        // awaitable
                        (true, true, true) => "await using ",
                        (true, true, false) => "using ",
                        (true, false, true) => "await using ",
                        (true, false, false) => "",
                        // sync
                        (false, true, true) => "using ",
                        (false, true, false) => "using ",
                        (false, false, true) => "__use_wrapper", // IAsyncDisposable but sync, needs special wrapper
                        (false, false, false) => ""
                    };

                    if (usingInstance != "__use_wrapper")
                    {
                        sb.AppendLine($"{usingInstance}var instance = {command.CommandMethodInfo.BuildNew()};");
                        invokeCommand = $"instance.{command.CommandMethodInfo.MethodName}({methodArguments})";
                    }
                    else
                    {
                        sb.AppendLine($"using var instance = new SyncAsyncDisposeWrapper<{command.CommandMethodInfo.TypeFullName}>({command.CommandMethodInfo.BuildNew()});");
                        invokeCommand = $"instance.Value.{command.CommandMethodInfo.MethodName}({methodArguments})";
                    }
                }

                if (hasCancellationToken)
                {
                    invokeCommand = $"Task.Run(() => {invokeCommand}).WaitAsync(posixSignalHandler.TimeoutToken)";
                }
                if (command.IsAsync || hasCancellationToken)
                {
                    if (isRunAsync)
                    {
                        invokeCommand = $"await {invokeCommand}";
                    }
                    else
                    {
                        invokeCommand = $"{invokeCommand}.GetAwaiter().GetResult()";
                    }
                }

                if (command.IsVoid)
                {
                    sb.AppendLine($"{invokeCommand};");
                }
                else
                {
                    sb.AppendLine($"Environment.ExitCode = {invokeCommand};");
                }
            }

            if (!command.HasFilter)
            {
                using (sb.BeginBlock("catch (Exception ex)"))
                {
                    if (hasCancellationToken)
                    {
                        using (sb.BeginBlock("if (ex is OperationCanceledException)"))
                        {
                            sb.AppendLine("Environment.ExitCode = 130;");
                            sb.AppendLine("return;");
                        }
                        sb.AppendLine();
                    }

                    sb.AppendLine("Environment.ExitCode = 1;");

                    using (sb.BeginBlock("if (ex is ValidationException or ArgumentParseFailedException)"))
                    {
                        sb.AppendLine("LogError(ex.Message);");
                    }
                    using (sb.BeginBlock("else"))
                    {
                        sb.AppendLine("LogError(ex.ToString());");
                    }
                }
                if (!emitForBuilder)
                {
                    using (sb.BeginBlock("finally"))
                    {
                        if (!isRunAsync)
                        {
                            using (sb.BeginBlock("if (ServiceProvider is IDisposable d)"))
                            {
                                sb.AppendLine("d.Dispose();");
                            }
                        }
                        else
                        {
                            using (sb.BeginBlock("if (ServiceProvider is IAsyncDisposable ad)"))
                            {
                                sb.AppendLine("await ad.DisposeAsync();");
                            }
                            using (sb.BeginBlock("else if (ServiceProvider is IDisposable d)"))
                            {
                                sb.AppendLine("d.Dispose();");
                            }
                        }
                    }
                }
            }
        }
    }

    public void EmitBuilder(SourceBuilder sb, CommandWithId[] commandIds, bool emitSync, bool emitAsync)
    {
        // grouped by path
        var commandGroup = commandIds.ToLookup(x => x.Command.Name.Split(' ')[0]);
        var hasRootCommand = commandIds.Any(x => x.Command.IsRootCommand);

        using (sb.BeginBlock("partial class ConsoleAppBuilder"))
        {
            // fields: 'Action command0 = default!;'
            foreach (var item in commandIds.Where(x => x.FieldType != null))
            {
                sb.AppendLine($"{item.FieldType} command{item.Id} = default!;");
            }

            // AddCore
            sb.AppendLine();
            using (sb.BeginBlock("partial void AddCore(string commandName, Delegate command)"))
            {
                using (sb.BeginBlock("switch (commandName)"))
                {
                    foreach (var item in commandIds.Where(x => x.FieldType != null))
                    {
                        using (sb.BeginIndent($"case \"{item.Command.Name}\":"))
                        {
                            sb.AppendLine($"this.command{item.Id} = Unsafe.As<{item.FieldType}>(command);");
                            sb.AppendLine("break;");
                        }
                    }
                    using (sb.BeginIndent("default:"))
                    {
                        sb.AppendLine("break;");
                    }
                }
            }

            // RunCore
            if (emitSync)
            {
                sb.AppendLine();
                using (sb.BeginBlock("partial void RunCore(string[] args, CancellationToken cancellationToken)"))
                {
                    if (hasRootCommand)
                    {
                        using (sb.BeginBlock("if (args.Length == 1 && args[0] is \"--help\" or \"-h\")"))
                        {
                            sb.AppendLine("ShowHelp(-1);");
                            sb.AppendLine("return;");
                        }
                    }

                    EmitRunBody(commandGroup, 0, false);
                }
            }

            // RunAsyncCore
            if (emitAsync)
            {
                sb.AppendLine();
                using (sb.BeginBlock("partial void RunAsyncCore(string[] args, CancellationToken cancellationToken, ref Task result)"))
                {
                    if (hasRootCommand)
                    {
                        using (sb.BeginBlock("if (args.Length == 1 && args[0] is \"--help\" or \"-h\")"))
                        {
                            sb.AppendLine("ShowHelp(-1);");
                            sb.AppendLine("return;");
                        }
                    }

                    EmitRunBody(commandGroup, 0, true);
                }
            }

            // static sync command function
            HashSet<Command> emittedCommand = new();
            if (emitSync)
            {
                sb.AppendLine();
                foreach (var item in commandIds)
                {
                    if (!emittedCommand.Add(item.Command)) continue;

                    if (item.Command.HasFilter)
                    {
                        EmitRun(sb, item, true, $"RunCommand{item.Id}Async");
                    }
                    else
                    {
                        EmitRun(sb, item, false, $"RunCommand{item.Id}");
                    }
                }
            }

            // static async command function
            if (emitAsync)
            {
                sb.AppendLine();
                foreach (var item in commandIds)
                {
                    if (!emittedCommand.Add(item.Command)) continue;
                    EmitRun(sb, item, true, $"RunCommand{item.Id}Async");
                }
            }

            // filter invoker
            foreach (var item in commandIds)
            {
                if (item.Command.HasFilter)
                {
                    sb.AppendLine();
                    EmitFilterInvoker(item);
                }
            }
        }

        void EmitRunBody(ILookup<string, CommandWithId> groupedCommands, int depth, bool isRunAsync)
        {
            var leafCommand = groupedCommands[""].FirstOrDefault();
            IDisposable? ifBlock = null;
            if (!(groupedCommands.Count == 1 && leafCommand != null))
            {
                ifBlock = sb.BeginBlock($"if (args.Length == {depth})");
            }
            EmitLeafCommand(leafCommand);
            if (ifBlock != null)
            {
                sb.AppendLine("return;");
                ifBlock.Dispose();
            }
            else
            {
                return;
            }

            using (sb.BeginBlock($"switch (args[{depth}])"))
            {
                foreach (var commands in groupedCommands.Where(x => x.Key != ""))
                {
                    using (sb.BeginIndent($"case \"{commands.Key}\":"))
                    {
                        var nextDepth = depth + 1;
                        var nextGroup = commands
                            .ToLookup(x =>
                            {
                                var path = x.Command.Name.Split(' ');
                                if (path.Length > nextDepth)
                                {
                                    return path[nextDepth];
                                }
                                else
                                {
                                    return ""; // as leaf command
                                }
                            });

                        EmitRunBody(nextGroup, nextDepth, isRunAsync); // recursive
                        sb.AppendLine("break;");
                    }
                }

                using (sb.BeginIndent("default:"))
                {
                    var leafCommand2 = groupedCommands[""].FirstOrDefault();
                    EmitLeafCommand(leafCommand2);
                    sb.AppendLine("break;");
                }
            }

            void EmitLeafCommand(CommandWithId? command)
            {
                if (command == null)
                {
                    sb.AppendLine($"if (!TryShowHelpOrVersion(args.AsSpan({depth}), -1, -1)) ShowHelp(-1);");
                }
                else
                {
                    string commandArgs = "";
                    if (command.Command.DelegateBuildType != DelegateBuildType.None)
                    {
                        commandArgs = $", command{command.Id}";
                    }

                    if (!command.Command.HasFilter)
                    {
                        if (!isRunAsync)
                        {
                            sb.AppendLine($"RunCommand{command.Id}(args, {depth}, args.AsSpan().IndexOf(\"--\"){commandArgs}, cancellationToken);");
                        }
                        else
                        {
                            sb.AppendLine($"result = RunCommand{command.Id}Async(args, {depth}, args.AsSpan().IndexOf(\"--\"){commandArgs}, cancellationToken);");
                        }
                    }
                    else
                    {
                        var invokerArgument = commandArgs.TrimStart(',', ' ');
                        invokerArgument = (invokerArgument != "") ? $"this, {invokerArgument}" : "this";
                        var invokeCode = $"RunWithFilterAsync(\"{command.Command.Name}\", args, {depth}, args.AsSpan().IndexOf(\"--\"), new Command{command.Id}Invoker({invokerArgument}).BuildFilter(), cancellationToken)";
                        if (!isRunAsync)
                        {
                            sb.AppendLine($"{invokeCode}.GetAwaiter().GetResult();");
                        }
                        else
                        {
                            sb.AppendLine($"result = {invokeCode};");
                        }
                    }
                }
            }
        }

        void EmitFilterInvoker(CommandWithId command)
        {
            var commandType = command.Command.BuildDelegateSignature(command.BuildCustomDelegateTypeName(), out _);
            var needsCommand = commandType != null;
            if (needsCommand) commandType = $"{commandType} command";
            if (!string.IsNullOrEmpty(commandType)) commandType = ", " + commandType;

            using (sb.BeginBlock($"sealed class Command{command.Id}Invoker(ConsoleAppBuilder builder{commandType}) : ConsoleAppFilter(null!)"))
            {
                using (sb.BeginBlock($"public ConsoleAppFilter BuildFilter()"))
                {
                    var i = -1;
                    foreach (var filter in command.Command.Filters.Reverse())
                    {
                        var newFilter = filter.BuildNew(i == -1 ? "this" : $"filter{i}");
                        sb.AppendLine($"var filter{++i} = {newFilter};");
                    }
                    sb.AppendLine($"return filter{i};");
                }

                sb.AppendLine();
                using (sb.BeginBlock($"public override Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken)"))
                {
                    var cmdArgs = needsCommand ? ", command" : "";
                    sb.AppendLine($"return builder.RunCommand{command.Id}Async(context.InternalCommandArgs, context.CommandDepth, context.EscapeIndex{cmdArgs}, context, cancellationToken);");
                }
            }
        }
    }

    // for single root command(Run)
    public void EmitHelp(SourceBuilder sb, Command command)
    {
        using (sb.BeginBlock("static partial void ShowHelp(int helpId)"))
        {
            sb.AppendLine("Log(\"\"\"");
            sb.AppendWithoutIndent(CommandHelpBuilder.BuildRootHelpMessage(command));
            sb.AppendLineWithoutIndent("\"\"\");");
        }
    }

    // for multiple commands(Builder)
    public void EmitHelp(SourceBuilder sb, CommandWithId[] commands)
    {
        using (sb.BeginBlock("static partial void ShowHelp(int helpId)"))
        {
            using (sb.BeginBlock("switch (helpId)"))
            {
                foreach (var command in commands)
                {
                    using (sb.BeginIndent($"case {command.Id}:"))
                    {
                        sb.AppendLine("Log(\"\"\"");
                        sb.AppendWithoutIndent(CommandHelpBuilder.BuildCommandHelpMessage(command.Command));
                        sb.AppendLineWithoutIndent("\"\"\");");
                        sb.AppendLine("break;");
                    }
                }

                using (sb.BeginIndent("default:"))
                {
                    sb.AppendLine("Log(\"\"\"");
                    sb.AppendWithoutIndent(CommandHelpBuilder.BuildRootHelpMessage(commands.Select(x => x.Command).ToArray()));
                    sb.AppendLineWithoutIndent("\"\"\");");
                    sb.AppendLine("break;");
                }
            }
        }
    }

    public void EmitConfigure(SourceBuilder sb, DllReference dllReference)
    {
        // configuration
        if (dllReference.HasConfiguration)
        {
            sb.AppendLine("bool requireConfiguration;");
            sb.AppendLine("IConfiguration? configuration;");

            if (dllReference.HasJsonConfiguration)
            {
                sb.AppendLine();
                sb.AppendLine("/// <summary>Create configuration with SetBasePath(Directory.GetCurrentDirectory()) and AddJsonFile(appsettings.json).</summary>");
                using (sb.BeginBlock("public ConsoleApp.ConsoleAppBuilder ConfigureDefaultConfiguration()"))
                {
                    sb.AppendLine("var config = new ConfigurationBuilder();");
                    sb.AppendLine("config.SetBasePath(System.IO.Directory.GetCurrentDirectory());");
                    sb.AppendLine("config.AddJsonFile(\"appsettings.json\", optional: true);");
                    sb.AppendLine("configuration = config.Build();");
                    sb.AppendLine("return this;");
                }

                sb.AppendLine();
                sb.AppendLine("/// <summary>Create configuration with SetBasePath(Directory.GetCurrentDirectory()) and AddJsonFile(appsettings.json).</summary>");
                using (sb.BeginBlock("public ConsoleApp.ConsoleAppBuilder ConfigureDefaultConfiguration(Action<IConfigurationBuilder> configure)"))
                {
                    sb.AppendLine("var config = new ConfigurationBuilder();");
                    sb.AppendLine("config.SetBasePath(System.IO.Directory.GetCurrentDirectory());");
                    sb.AppendLine("config.AddJsonFile(\"appsettings.json\", optional: true);");
                    sb.AppendLine("configure(config);");
                    sb.AppendLine("configuration = config.Build();");
                    sb.AppendLine("return this;");
                }
            }

            sb.AppendLine();
            using (sb.BeginBlock("public ConsoleApp.ConsoleAppBuilder ConfigureEmptyConfiguration(Action<IConfigurationBuilder> configure)"))
            {
                sb.AppendLine("var config = new ConfigurationBuilder();");
                sb.AppendLine("configure(config);");
                sb.AppendLine("configuration = config.Build();");
                sb.AppendLine("return this;");
            }
            sb.AppendLine();
        }

        // DependencyInjection
        if (dllReference.HasDependencyInjection)
        {
            if (dllReference.HasConfiguration)
            {
                sb.AppendLine("Action<IConfiguration, IServiceCollection>? configureServices;");
            }
            else
            {
                sb.AppendLine("Action<IServiceCollection>? configureServices;");
            }

            sb.AppendLine();
            using (sb.BeginBlock("public ConsoleApp.ConsoleAppBuilder ConfigureServices(Action<IServiceCollection> configure)"))
            {
                if (dllReference.HasConfiguration)
                {
                    sb.AppendLine("this.configureServices = (_, services) => configure(services);");
                }
                else
                {
                    sb.AppendLine("this.configureServices = configure;");
                }
                sb.AppendLine("return this;");
            }

            if (dllReference.HasConfiguration)
            {
                sb.AppendLine();
                using (sb.BeginBlock("public ConsoleApp.ConsoleAppBuilder ConfigureServices(Action<IConfiguration, IServiceCollection> configure)"))
                {
                    sb.AppendLine("this.requireConfiguration = true;");
                    sb.AppendLine("this.configureServices = configure;");
                    sb.AppendLine("return this;");
                }
            }
            sb.AppendLine();
        }

        // Logging
        if (dllReference.HasLogging)
        {
            if (dllReference.HasConfiguration)
            {
                sb.AppendLine("Action<IConfiguration, ILoggingBuilder>? configureLogging;");
            }
            else
            {
                sb.AppendLine("Action<ILoggingBuilder>? configureLogging;");
            }

            sb.AppendLine();
            using (sb.BeginBlock("public ConsoleApp.ConsoleAppBuilder ConfigureLogging(Action<ILoggingBuilder> configure)"))
            {
                if (dllReference.HasConfiguration)
                {
                    sb.AppendLine("this.configureLogging = (_, logging) => configure(logging);");
                }
                else
                {
                    sb.AppendLine("this.configureLogging = configure;");
                }
                sb.AppendLine("return this;");
            }

            if (dllReference.HasConfiguration)
            {
                sb.AppendLine();
                using (sb.BeginBlock("public ConsoleApp.ConsoleAppBuilder ConfigureLogging(Action<IConfiguration, ILoggingBuilder> configure)"))
                {
                    sb.AppendLine("this.requireConfiguration = true;");
                    sb.AppendLine("this.configureLogging = configure;");
                    sb.AppendLine("return this;");
                }
            }
            sb.AppendLine();
        }

        // Build
        using (sb.BeginBlock("partial void BuildAndSetServiceProvider(ConsoleAppContext context)"))
        {
            if (dllReference.HasDependencyInjection && dllReference.HasLogging)
            {
                sb.AppendLine("if (configureServices == null && configureLogging == null) return;");
            }
            else if (dllReference.HasDependencyInjection)
            {
                sb.AppendLine("if (configureServices == null) return;");
            }

            if (dllReference.HasDependencyInjection)
            {
                if (dllReference.HasConfiguration)
                {
                    sb.AppendLine("var config = configuration;");
                    using (sb.BeginBlock("if (requireConfiguration && config == null)"))
                    {
                        sb.AppendLine("config = new ConfigurationRoot(Array.Empty<IConfigurationProvider>());");
                    }
                }

                sb.AppendLine("var services = new ServiceCollection();");
                if (dllReference.HasConfiguration)
                {
                    sb.AppendLine("configureServices?.Invoke(configuration!, services);");
                }
                else
                {
                    sb.AppendLine("configureServices?.Invoke(services);");
                }

                if (dllReference.HasLogging)
                {
                    using (sb.BeginBlock("if (configureLogging != null)"))
                    {
                        sb.AppendLine("var configure = configureLogging;");
                        using (sb.BeginIndent("services.AddLogging(logging => {"))
                        {
                            if (dllReference.HasConfiguration)
                            {
                                sb.AppendLine("configure!(config!, logging);");
                            }
                            else
                            {
                                sb.AppendLine("configure!(logging);");
                            }
                        }
                        sb.AppendLine("});");
                    }
                }

                sb.AppendLine("ConsoleApp.ServiceProvider = services.BuildServiceProvider();");
            }
        }

        // HostStart(for filter)
        if (dllReference.HasHost)
        {
            sb.AppendLine();
            using (sb.BeginBlock("partial void StartHostAsyncIfNeeded(CancellationToken cancellationToken, ref Task task)"))
            {
                sb.AppendLine("Microsoft.Extensions.Hosting.IHost? host = ConsoleApp.ServiceProvider?.GetService(typeof(Microsoft.Extensions.Hosting.IHost)) as Microsoft.Extensions.Hosting.IHost;");
                using (sb.BeginBlock("if (startHost && host != null)"))
                {
                    sb.AppendLine("task = host.StartAsync(cancellationToken);");
                }
            }
        }
    }

    public void EmitAsConsoleAppBuilder(SourceBuilder sb, DllReference dllReference)
    {
        sb.AppendLine("""

internal static class ConsoleAppHostBuilderExtensions
{
    class CompositeDisposableServiceProvider(IDisposable host, IServiceProvider serviceServiceProvider, IDisposable scope, IServiceProvider serviceProvider)
        : IServiceProvider, IKeyedServiceProvider, IDisposable, IAsyncDisposable
    {
        public object? GetService(Type serviceType)
        {
            return serviceProvider.GetService(serviceType);
        }

        public object? GetKeyedService(Type serviceType, object? serviceKey)
        {
            return ((IKeyedServiceProvider)serviceProvider).GetKeyedService(serviceType, serviceKey);
        }

        public object GetRequiredKeyedService(Type serviceType, object? serviceKey)
        {
            return ((IKeyedServiceProvider)serviceProvider).GetRequiredKeyedService(serviceType, serviceKey);
        }

        public void Dispose()
        {
            if (serviceProvider is IDisposable d)
            {
                d.Dispose();
            }
            scope.Dispose();
            if (serviceServiceProvider is IDisposable d2)
            {
                d2.Dispose();
            }
            host.Dispose();
        }
        
        public async ValueTask DisposeAsync()
        {
            await CastAndDispose(serviceProvider);
            await CastAndDispose(scope);
            await CastAndDispose(serviceServiceProvider);
            await CastAndDispose(host);
            
            static async ValueTask CastAndDispose<T>(T resource)
            {
                if (resource is IAsyncDisposable resourceAsyncDisposable)
                {
                    await resourceAsyncDisposable.DisposeAsync();
                }
                else if (resource is IDisposable resourceDisposable)
                {
                    resourceDisposable.Dispose();
                }
            }
        }
    }

    internal static ConsoleApp.ConsoleAppBuilder ToConsoleAppBuilder(this IHostBuilder hostBuilder)
    {
        var host = hostBuilder.Build();
        var serviceServiceProvider = host.Services;
        var scope = serviceServiceProvider.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        ConsoleApp.ServiceProvider = new CompositeDisposableServiceProvider(host, serviceServiceProvider, scope, serviceProvider);
                
        return ConsoleApp.Create();
    }

    internal static ConsoleApp.ConsoleAppBuilder ToConsoleAppBuilder(this HostApplicationBuilder hostBuilder)
    {
        var host = hostBuilder.Build();
        var serviceServiceProvider = host.Services;
        var scope = serviceServiceProvider.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        ConsoleApp.ServiceProvider = new CompositeDisposableServiceProvider(host, serviceServiceProvider, scope, serviceProvider);
                
        return ConsoleApp.Create();
    }
}
""");
    }

    internal record CommandWithId(string? FieldType, Command Command, int Id)
    {
        public static string BuildCustomDelegateTypeName(int id)
        {
            if (id < 0) return "DelegateCommand";
            return $"DelegateCommand{id}";
        }

        public string BuildCustomDelegateTypeName()
        {
            return BuildCustomDelegateTypeName(Id);
        }
    }
}
