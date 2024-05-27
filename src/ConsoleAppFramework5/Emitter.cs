using Microsoft.CodeAnalysis;
using System.Reflection.Metadata;
using System.Text;

namespace ConsoleAppFramework;

internal class Emitter(WellKnownTypes wellKnownTypes)
{
    public string EmitRun(Command command, bool isRunAsync, string? methodName = null)
    {
        var emitForBuilder = methodName != null;
        var hasCancellationToken = command.Parameters.Any(x => x.IsCancellationToken);
        var hasArgument = command.Parameters.Any(x => x.IsArgument);
        var hasValidation = command.Parameters.Any(x => x.HasValidation);
        var parsableParameterCount = command.Parameters.Count(x => x.IsParsable);

        // prepare argument variables ->
        var prepareArgument = new SourceBuilder(2);
        if (hasCancellationToken)
        {
            prepareArgument.AppendLine("using var posixSignalHandler = PosixSignalHandler.Register(Timeout);");
        }
        for (var i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            if (parameter.IsParsable)
            {
                var defaultValue = parameter.HasDefaultValue ? parameter.DefaultValueToString() : $"default({parameter.Type.ToFullyQualifiedFormatDisplayString()})";
                prepareArgument.AppendLine($"var arg{i} = {defaultValue};");
                if (!parameter.HasDefaultValue)
                {
                    prepareArgument.AppendLine($"var arg{i}Parsed = false;");
                }
            }
            else if (parameter.IsCancellationToken)
            {
                prepareArgument.AppendLine($"var arg{i} = posixSignalHandler.Token;");
            }
            else if (parameter.IsFromServices)
            {
                var type = parameter.Type.ToFullyQualifiedFormatDisplayString();
                prepareArgument.AppendLine($"var arg{i} = ({type})ServiceProvider!.GetService(typeof({type}))!;");
            }
        }

        // parse indexed argument([Argument] parameter)
        var indexedArgument = new SourceBuilder(4);
        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            if (!parameter.IsArgument) continue;

            indexedArgument.AppendLine($"if (i == {parameter.ArgumentIndex})");
            using (indexedArgument.BeginBlock())
            {
                indexedArgument.AppendLine($"{parameter.BuildParseMethod(i, parameter.Name, wellKnownTypes, increment: false)}");
                if (!parameter.HasDefaultValue)
                {
                    indexedArgument.AppendLine($"arg{i}Parsed = true;");
                }
                indexedArgument.AppendLine("continue;");
            }
        }

        // parse argument(fast, switch directly) ->
        var fastParseCase = new SourceBuilder(5);
        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            if (!parameter.IsParsable) continue;
            if (parameter.IsArgument) continue;

            fastParseCase.AppendLine($"case \"--{parameter.Name}\":");
            foreach (var alias in parameter.Aliases)
            {
                fastParseCase.AppendLine($"case \"{alias}\":");
            }
            using (fastParseCase.BeginBlock())
            {
                fastParseCase.AppendLine($"{parameter.BuildParseMethod(i, parameter.Name, wellKnownTypes, increment: true)}");
                if (!parameter.HasDefaultValue)
                {
                    fastParseCase.AppendLine($"arg{i}Parsed = true;");
                }
                fastParseCase.AppendLine("break;");
            }
        }

        // parse argument(slow, if ignorecase) ->
        var slowIgnoreCaseParse = new SourceBuilder(6);
        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            if (!parameter.IsParsable) continue;
            if (parameter.IsArgument) continue;

            slowIgnoreCaseParse.AppendLine($"if (string.Equals(name, \"--{parameter.Name}\", StringComparison.OrdinalIgnoreCase){(parameter.Aliases.Length == 0 ? ")" : "")}");
            for (int j = 0; j < parameter.Aliases.Length; j++)
            {
                var alias = parameter.Aliases[j];
                slowIgnoreCaseParse.AppendLine($" || string.Equals(name, \"{alias}\", StringComparison.OrdinalIgnoreCase){(parameter.Aliases.Length == j + 1 ? ")" : "")}");
            }
            using (slowIgnoreCaseParse.BeginBlock())
            {
                slowIgnoreCaseParse.AppendLine($"{parameter.BuildParseMethod(i, parameter.Name, wellKnownTypes, increment: true)}");
                if (!parameter.HasDefaultValue)
                {
                    slowIgnoreCaseParse.AppendLine($"arg{i}Parsed = true;");
                }
                slowIgnoreCaseParse.AppendLine($"break;");
            }
        }

        // validate parsed ->
        var validateParsed = new SourceBuilder(3);
        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            if (!parameter.IsParsable) continue;

            if (!parameter.HasDefaultValue)
            {
                validateParsed.AppendLine($"if (!arg{i}Parsed) ThrowRequiredArgumentNotParsed(\"{parameter.Name}\");");
            }
        }

        // hasValidation ->
        var attributeValidation = new SourceBuilder(3);
        if (hasValidation)
        {
            attributeValidation.AppendLine("var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(\"\", null, null);");
            attributeValidation.AppendLine("var parameters = command.Method.GetParameters();");
            attributeValidation.AppendLine("System.Text.StringBuilder? errorMessages = null;");
            for (int i = 0; i < command.Parameters.Length; i++)
            {
                var parameter = command.Parameters[i];
                if (!parameter.HasValidation) continue;

                attributeValidation.AppendLine($"ValidateParameter(arg{i}, parameters[{i}], validationContext, ref errorMessages);");
            }
            attributeValidation.AppendLine("if (errorMessages != null)");
            using (attributeValidation.BeginBlock())
            {
                attributeValidation.AppendLine("throw new System.ComponentModel.DataAnnotations.ValidationException(errorMessages.ToString());");
            }
        }

        // invoke for sync/async, void/int
        var invoke = new SourceBuilder(3);
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

            invoke.AppendLine($"{usingInstance}var instance = {command.CommandMethodInfo.BuildNew()};");
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
            invoke.AppendLine($"{invokeCommand};");
        }
        else
        {
            invoke.AppendLine($"Environment.ExitCode = {invokeCommand};");
        }
        invoke.Unindent();
        invoke.AppendLine("}"); // try close
        if (hasCancellationToken)
        {
            invoke.AppendLine("catch (OperationCanceledException ex) when (ex.CancellationToken == posixSignalHandler.Token || ex.CancellationToken == posixSignalHandler.TimeoutToken)");
            using (invoke.BeginBlock())
            {
                invoke.AppendLine("Environment.ExitCode = 130;");
            }
            invoke.Unindent();
            invoke.AppendLine("}");
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

        var code = $$"""
    {{accessibility}} static {{unsafeCode}}{{returnType}} {{methodName}}({{argsType}} args{{commandMethodType}})
    {
        if (TryShowHelpOrVersion(args, {{parsableParameterCount}})) return;

{{prepareArgument}}
        try
        {
            for (int i = 0; i < args.Length; i++)
            {
{{indexedArgument}}
                var name = args[i];

                switch (name)
                {
{{fastParseCase}}
                    default:
{{slowIgnoreCaseParse}}
                        ThrowArgumentNameNotFound(name);
                        break;
                }
            }
{{validateParsed}}
{{attributeValidation}}
{{invoke}}
        catch (Exception ex)
        {
            Environment.ExitCode = 1;
            if (ex is System.ComponentModel.DataAnnotations.ValidationException)
            {
                LogError(ex.Message);
            }
            else
            {
                LogError(ex.ToString());
            }
        }
    }
""";

        if (delegateType != null && !emitForBuilder)
        {
            code += $$"""


    internal {{delegateType}}
""";
        }

        return code;
    }

    public string EmitBuilder(Command[] commands, bool emitSync, bool emitAsync)
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

        var sb = new SourceBuilder(1);
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
        }

        // emit outside of ConsoleAppBuilder

        // static sync command function
        if (emitSync)
        {
            sb.AppendLine();
            foreach (var item in commandIds)
            {
                sb.AppendLine(EmitRun(item.Command, false, $"RunCommand{item.Id}").TrimStart());
            }
        }

        // static async command function
        if (emitAsync)
        {
            sb.AppendLine();
            foreach (var item in commandIds)
            {
                sb.AppendLine(EmitRun(item.Command, true, $"RunAsyncCommand{item.Id}").TrimStart());
            }
        }

        return sb.ToString();

        void EmitRunBody(IEnumerable<IGrouping<string?, CommandWithId>> groupedCommands, int depth, bool isRunAsync)
        {
            using (sb.BeginBlock($"switch (args[{depth}])"))
            {
                // case:...
                foreach (var commands in groupedCommands)
                {
                    using (sb.BeginIndent($"case \"{commands.Key}\":"))
                    {
                        // TODO: check leaf command
                        if (commands.Count() != 1)
                        {
                            // recursive: next depth
                            var nextDepth = depth + 1;
                            var nextGroup = commands.GroupBy(x => x.Command.CommandPath.Length < nextDepth ? x.Command.CommandName : x.Command.CommandPath[nextDepth]);
                            EmitRunBody(nextGroup, nextDepth, isRunAsync);
                            sb.AppendLine("break;");
                        }
                        else
                        {
                            var cmd = commands.First();

                            string commandArgs = "";
                            if (cmd.Command.DelegateBuildType != DelegateBuildType.None)
                            {
                                commandArgs = $", command{cmd.Id}";
                            }

                            if (!isRunAsync)
                            {
                                sb.AppendLine($"RunCommand{cmd.Id}(args.AsSpan({depth + 1}){commandArgs});");
                            }
                            else
                            {
                                sb.AppendLine($"result = RunAsyncCommand{cmd.Id}(args[1..]{commandArgs});");
                            }
                            sb.AppendLine("break;");
                        }
                    }
                }

                // TODO: invoke root command
                using (sb.BeginIndent("default:"))
                {
                    sb.AppendLine("break;");
                }
            }
        }
    }
}

internal record CommandWithId(string? FieldType, Command Command, int Id);
