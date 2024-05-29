using Microsoft.CodeAnalysis;
using System.Reflection.Metadata;

namespace ConsoleAppFramework;

internal class Emitter(WellKnownTypes wellKnownTypes)
{
    public void EmitRun(SourceBuilder sb, CommandWithId commandWithId, bool isRunAsync, string? methodName = null)
    {
        var command = commandWithId.Command;

        var emitForBuilder = methodName != null;
        var hasCancellationToken = command.Parameters.Any(x => x.IsCancellationToken);
        var hasArgument = command.Parameters.Any(x => x.IsArgument);
        var hasValidation = command.Parameters.Any(x => x.HasValidation);
        var parsableParameterCount = command.Parameters.Count(x => x.IsParsable);

        if (command.HasFilter)
        {
            isRunAsync = true;
            hasCancellationToken = false;
        }
        var returnType = isRunAsync ? "async Task" : "void";
        var accessibility = !emitForBuilder ? "public" : "private";
        var argsType = !emitForBuilder ? "string[]" : (isRunAsync ? "string[]" : "ReadOnlySpan<string>"); // NOTE: C# 13 will allow Span<T> in async methods so can change to ReadOnlyMemory<string>(and store .Span in local var)
        methodName = methodName ?? (isRunAsync ? "RunAsync" : "Run");
        var unsafeCode = (command.MethodKind == MethodKind.FunctionPointer) ? "unsafe " : "";

        var commandMethodType = command.BuildDelegateSignature(out var delegateType);
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

        var filterCancellationToken = command.HasFilter ? ", CancellationToken cancellationToken" : "";

        // method signature
        using (sb.BeginBlock($"{accessibility} static {unsafeCode}{returnType} {methodName}({argsType} args{commandMethodType}{filterCancellationToken})"))
        {
            sb.AppendLine($"if (TryShowHelpOrVersion(args, {parsableParameterCount}, {commandWithId.Id})) return;");
            sb.AppendLine();

            // prepare argument variables
            if (hasCancellationToken)
            {
                sb.AppendLine("using var posixSignalHandler = PosixSignalHandler.Register(Timeout);");
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
                else if (parameter.IsFromServices)
                {
                    var type = parameter.Type.ToFullyQualifiedFormatDisplayString();
                    sb.AppendLine($"var arg{i} = ({type})ServiceProvider!.GetService(typeof({type}))!;");
                }
            }
            sb.AppendLineIfExists(command.Parameters);

            using (command.HasFilter ? sb.Nop : sb.BeginBlock("try"))
            {
                using (sb.BeginBlock("for (int i = 0; i < args.Length; i++)"))
                {
                    // parse indexed argument([Argument] parameter)
                    if (hasArgument)
                    {
                        for (int i = 0; i < command.Parameters.Length; i++)
                        {
                            var parameter = command.Parameters[i];
                            if (!parameter.IsArgument) continue;

                            sb.AppendLine($"if (i == {parameter.ArgumentIndex})");
                            using (sb.BeginBlock())
                            {
                                sb.AppendLine($"{parameter.BuildParseMethod(i, parameter.Name, wellKnownTypes, increment: false)}");
                                if (parameter.RequireCheckArgumentParsed)
                                {
                                    sb.AppendLine($"arg{i}Parsed = true;");
                                }
                                sb.AppendLine("continue;");
                            }
                        }
                        sb.AppendLine();
                    }

                    sb.AppendLine("var name = args[i];");
                    sb.AppendLine();

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
                                sb.AppendLine($"{parameter.BuildParseMethod(i, parameter.Name, wellKnownTypes, increment: true)}");
                                if (parameter.RequireCheckArgumentParsed)
                                {
                                    sb.AppendLine($"arg{i}Parsed = true;");
                                }
                                sb.AppendLine("break;");
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
                                    sb.AppendLine($"{parameter.BuildParseMethod(i, parameter.Name, wellKnownTypes, increment: true)}");
                                    if (parameter.RequireCheckArgumentParsed)
                                    {
                                        sb.AppendLine($"arg{i}Parsed = true;");
                                    }
                                    sb.AppendLine($"break;");
                                }
                            }

                            sb.AppendLine("ThrowArgumentNameNotFound(name);");
                            sb.AppendLine("break;");
                        }
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
                    sb.AppendLine("var parameters = command.Method.GetParameters();");
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
                        (false, false, true) => "", // IAsyncDisposable but sync, can't call disposeasync......
                        (false, false, false) => ""
                    };

                    sb.AppendLine($"{usingInstance}var instance = {command.CommandMethodInfo.BuildNew()};");
                    invokeCommand = $"instance.{command.CommandMethodInfo.MethodName}({methodArguments})";
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
                        using (sb.BeginBlock("if ((ex is OperationCanceledException oce) && (oce.CancellationToken == posixSignalHandler.Token || oce.CancellationToken == posixSignalHandler.TimeoutToken))"))
                        {
                            sb.AppendLine("Environment.ExitCode = 130;");
                            sb.AppendLine("return;");
                        }
                        sb.AppendLine();
                    }

                    sb.AppendLine("Environment.ExitCode = 1;");

                    using (sb.BeginBlock("if (ex is ValidationException)"))
                    {
                        sb.AppendLine("LogError(ex.Message);");
                    }
                    using (sb.BeginBlock("else"))
                    {
                        sb.AppendLine("LogError(ex.ToString());");
                    }
                }
            }
        }
    }

    public void EmitBuilder(SourceBuilder sb, Command[] commands, bool emitSync, bool emitAsync)
    {
        // with id number
        var commandIds = commands
            .Select((x, i) =>
            {
                return new CommandWithId(
                    FieldType: x.BuildDelegateSignature(out _), // for builder, always generate Action/Func so ok to ignore out var.
                    Command: x,
                    Id: i
                );
            })
            .ToArray();

        // grouped by path
        var commandGroup = commandIds.ToLookup(x => x.Command.CommandPath.Length == 0 ? x.Command.CommandName : x.Command.CommandPath[0]);

        using (sb.BeginBlock("partial struct ConsoleAppBuilder"))
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
                        using (sb.BeginIndent($"case \"{item.Command.CommandFullName}\":"))
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
                using (sb.BeginBlock("partial void RunCore(string[] args)"))
                {
                    EmitRunBody(commandGroup, 0, false);
                }
            }

            // RunAsyncCore
            if (emitAsync)
            {
                sb.AppendLine();
                using (sb.BeginBlock("partial void RunAsyncCore(string[] args, ref Task result)"))
                {
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
            IDisposable? ifBlcok = null;
            if (!(groupedCommands.Count == 1 && leafCommand != null))
            {
                ifBlcok = sb.BeginBlock($"if (args.Length == {depth})");
            }
            EmitLeafCommand(leafCommand);
            if (ifBlcok != null)
            {
                sb.AppendLine("return;");
                ifBlcok.Dispose();
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
                                var len = x.Command.CommandPath.Length;
                                if (len > nextDepth)
                                {
                                    return x.Command.CommandPath[nextDepth];
                                }
                                if (len == nextDepth)
                                {
                                    return x.Command.CommandName;
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
                    sb.AppendLine("TryShowHelpOrVersion(args, -1, -1);");
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
                            sb.AppendLine($"RunCommand{command.Id}(args.AsSpan({depth}){commandArgs});");
                        }
                        else
                        {
                            sb.AppendLine($"result = RunCommand{command.Id}Async(args[{depth}..]{commandArgs});");
                        }
                    }
                    else
                    {
                        var invokeCode = $"RunWithFilterAsync(new Command{command.Id}Invoker(args[{depth}..]{commandArgs}).BuildFilter())";
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
            var commandType = command.Command.BuildDelegateSignature(out _);
            var needsCommand = commandType != null;
            if (needsCommand) commandType = $", {commandType} command";

            using (sb.BeginBlock($"sealed class Command{command.Id}Invoker(string[] args{commandType}) : ConsoleAppFilter(null!)"))
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
                using (sb.BeginBlock($"public override Task InvokeAsync(CancellationToken cancellationToken)"))
                {
                    var cmdArgs = needsCommand ? ", command" : "";
                    sb.AppendLine($"return RunCommand{command.Id}Async(args{cmdArgs}, cancellationToken);");
                }
            }
        }
    }

    public void EmitHelp(SourceBuilder sb, Command command)
    {
        var helpBuilder = new CommandHelpBuilder();
        var help = helpBuilder.BuildHelpMessage(command);
        using (sb.BeginBlock("static partial void ShowHelp(int helpId)"))
        {
            sb.AppendLine("Log(\"\"\"");
            sb.AppendLineWithoutIndent(help);
            sb.AppendLineWithoutIndent("\"\"\");");
        }
    }

    internal record CommandWithId(string? FieldType, Command Command, int Id);
}
