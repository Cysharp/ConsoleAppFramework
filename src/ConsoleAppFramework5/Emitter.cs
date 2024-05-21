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
        var prepareArgument = new StringBuilder();
        if (hasCancellationToken)
        {
            prepareArgument.AppendLine("        using var posixSignalHandler = PosixSignalHandler.Register(Timeout);");
        }
        for (var i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            if (parameter.IsParsable)
            {
                var defaultValue = parameter.HasDefaultValue ? parameter.DefaultValueToString() : $"default({parameter.Type.ToFullyQualifiedFormatDisplayString()})";
                prepareArgument.AppendLine($"        var arg{i} = {defaultValue};");
                if (!parameter.HasDefaultValue)
                {
                    prepareArgument.AppendLine($"        var arg{i}Parsed = false;");
                }
            }
            else if (parameter.IsCancellationToken)
            {
                prepareArgument.AppendLine($"        var arg{i} = posixSignalHandler.Token;");
            }
            else if (parameter.IsFromServices)
            {
                var type = parameter.Type.ToFullyQualifiedFormatDisplayString();
                prepareArgument.AppendLine($"        var arg{i} = ({type})ServiceProvider!.GetService(typeof({type}))!;");
            }
        }

        // parse indexed argument([Argument] parameter)
        var indexedArgument = new StringBuilder();
        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            if (!parameter.IsArgument) continue;

            indexedArgument.AppendLine($"                if (i == {parameter.ArgumentIndex})");
            indexedArgument.AppendLine("                {");
            indexedArgument.AppendLine($"                    {parameter.BuildParseMethod(i, parameter.Name, wellKnownTypes, increment: false)}");
            if (!parameter.HasDefaultValue)
            {
                indexedArgument.AppendLine($"                    arg{i}Parsed = true;");
            }
            indexedArgument.AppendLine("                    continue;");
            indexedArgument.AppendLine("                }");
        }

        // parse argument(fast, switch directly) ->
        var fastParseCase = new StringBuilder();
        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            if (!parameter.IsParsable) continue;
            if (parameter.IsArgument) continue;

            fastParseCase.AppendLine($"                    case \"--{parameter.Name}\":");
            foreach (var alias in parameter.Aliases)
            {
                fastParseCase.AppendLine($"                    case \"{alias}\":");
            }
            fastParseCase.AppendLine("                    {");
            fastParseCase.AppendLine($"                        {parameter.BuildParseMethod(i, parameter.Name, wellKnownTypes, increment: true)}");
            if (!parameter.HasDefaultValue)
            {
                fastParseCase.AppendLine($"                        arg{i}Parsed = true;");
            }
            fastParseCase.AppendLine("                        break;");
            fastParseCase.AppendLine("                    }");
        }

        // parse argument(slow, if ignorecase) ->
        var slowIgnoreCaseParse = new StringBuilder();
        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            if (!parameter.IsParsable) continue;
            if (parameter.IsArgument) continue;

            slowIgnoreCaseParse.AppendLine($"                        if (string.Equals(name, \"--{parameter.Name}\", StringComparison.OrdinalIgnoreCase){(parameter.Aliases.Length == 0 ? ")" : "")}");
            for (int j = 0; j < parameter.Aliases.Length; j++)
            {
                var alias = parameter.Aliases[j];
                slowIgnoreCaseParse.AppendLine($"                         || string.Equals(name, \"{alias}\", StringComparison.OrdinalIgnoreCase){(parameter.Aliases.Length == j + 1 ? ")" : "")}");
            }
            slowIgnoreCaseParse.AppendLine("                        {");
            slowIgnoreCaseParse.AppendLine($"                            {parameter.BuildParseMethod(i, parameter.Name, wellKnownTypes, increment: true)}");
            if (!parameter.HasDefaultValue)
            {
                slowIgnoreCaseParse.AppendLine($"                            arg{i}Parsed = true;");
            }
            slowIgnoreCaseParse.AppendLine($"                            break;");
            slowIgnoreCaseParse.AppendLine("                        }");
        }

        // validate parsed ->
        var validateParsed = new StringBuilder();
        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            if (!parameter.IsParsable) continue;

            if (!parameter.HasDefaultValue)
            {
                validateParsed.AppendLine($"            if (!arg{i}Parsed) ThrowRequiredArgumentNotParsed(\"{parameter.Name}\");");
            }
        }

        // hasValidation ->
        var attributeValidation = new StringBuilder();
        if (hasValidation)
        {
            attributeValidation.AppendLine("            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(\"\", null, null);");
            attributeValidation.AppendLine("            var parameters = command.Method.GetParameters();");
            attributeValidation.AppendLine("            System.Text.StringBuilder? errorMessages = null;");
            for (int i = 0; i < command.Parameters.Length; i++)
            {
                var parameter = command.Parameters[i];
                if (!parameter.HasValidation) continue;

                attributeValidation.AppendLine($"            ValidateParameter(arg{i}, parameters[{i}], validationContext, ref errorMessages);");
            }
            attributeValidation.AppendLine("            if (errorMessages != null)");
            attributeValidation.AppendLine("            {");
            attributeValidation.AppendLine("                throw new System.ComponentModel.DataAnnotations.ValidationException(errorMessages.ToString());");
            attributeValidation.AppendLine("            }");
        }

        // invoke for sync/async, void/int
        var invoke = new StringBuilder();
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

            invoke.AppendLine($"            {usingInstance}var instance = {command.CommandMethodInfo.BuildNew()};");
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
            invoke.AppendLine($"            {invokeCommand};");
        }
        else
        {
            invoke.AppendLine($"            Environment.ExitCode = {invokeCommand};");
        }
        invoke.AppendLine("        }"); // try close
        if (hasCancellationToken)
        {
            invoke.AppendLine("        catch (OperationCanceledException ex) when (ex.CancellationToken == posixSignalHandler.Token || ex.CancellationToken == posixSignalHandler.TimeoutToken)");
            invoke.AppendLine("        {");
            invoke.AppendLine("            Environment.ExitCode = 130;");
            invoke.AppendLine("        }");
        }

        var returnType = isRunAsync ? "async Task" : "void";
        var accessibility = !emitForBuilder ? "public" : "private";
        var argsType = !emitForBuilder ? "string[]" : (isRunAsync ? "string[]" : "ReadOnlySpan<string>"); // NOTE: C# 13 will allow Span<T> in async methods so can change to ReadOnlyMemory<string>(and store .Span in local var)
        methodName = methodName ?? (isRunAsync ? "RunAsync" : "Run");
        var unsafeCode = (command.MethodKind == MethodKind.FunctionPointer) ? "unsafe " : "";

        var commandMethodType = command.BuildDelegateSignature(out var delegateType);
        if (commandMethodType != "")
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
        // TODO: make Add -> make Run -> make RunAsync
        // TODO: invoke RootCommand
        var fields = new StringBuilder();
        var addCase = new StringBuilder();
        var runCommands = new StringBuilder();
        var runAsyncCommands = new StringBuilder();
        var runCase = new StringBuilder();
        var runAsyncCase = new StringBuilder();

        for (int i = 0; i < commands.Length; i++)
        {
            var command = commands[i];

            string commandArgs = "";
            if (command.DelegateBuildType != DelegateBuildType.None)
            {
                var fieldType = command.BuildDelegateSignature(out _); // for builder, always generate Action/Func.
                fields.AppendLine($"    {fieldType} command{i} = default!;");

                addCase.AppendLine($"            case \"{command.CommandName}\":");
                addCase.AppendLine($"                this.command{i} = Unsafe.As<{fieldType}>(command);");
                addCase.AppendLine($"                break;");

                commandArgs = $", command{i}";
            }

            if (emitSync)
            {
                runCase.AppendLine($"            case \"{command.CommandName}\":");
                runCase.AppendLine($"                RunCommand{i}(args.AsSpan(1){commandArgs});");
                runCase.AppendLine($"                break;");
                runCommands.AppendLine(EmitRun(command, false, $"RunCommand{i}"));
            }

            if (emitAsync)
            {
                runAsyncCase.AppendLine($"            case \"{command.CommandName}\":");
                runAsyncCase.AppendLine($"                result = RunAsyncCommand{i}(args[1..]{commandArgs});");
                runAsyncCase.AppendLine($"                break;");
                runAsyncCommands.AppendLine(EmitRun(command, true, $"RunAsyncCommand{i}"));
            }
        }

        var addCore = $$"""
    partial void AddCore(string commandName, Delegate command)
    {
        switch (commandName)
        {
{{addCase}}
            default:
                break;
        }
    }
""";

        var runCore = $$"""
    partial void RunCore(string[] args)
    {
        switch (args[0])
        {
{{runCase}}
            default:
                break;
        }
    }
""";

        var runAsyncCore = $$"""
    partial void RunAsyncCore(string[] args, ref Task result)
    {
        switch (args[0])
        {
{{runAsyncCase}}
            default:
                break;
        }
    }
""";

        if (!emitSync) runCore = "";
        if (!emitAsync) runAsyncCore = "";

        // TODO: Emit help and version
        var code = $$"""
partial struct ConsoleAppBuilder
{
{{fields}}

{{addCore}}

{{runCommands}}
{{runAsyncCommands}}

{{runCore}}
{{runAsyncCore}}
}
""";

        return code;
    }
}
