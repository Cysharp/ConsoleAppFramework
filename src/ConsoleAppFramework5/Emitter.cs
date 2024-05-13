﻿using Microsoft.CodeAnalysis;
using System.Text;

namespace ConsoleAppFramework;

internal class Emitter(Command command, WellKnownTypes wellKnownTypes)
{
    public string EmitRun(bool isRunAsync)
    {
        var hasCancellationToken = command.Parameters.Any(x => x.IsCancellationToken);

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

        // parse argument(fast, switch directly) ->
        var fastParseCase = new StringBuilder();
        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            if (!parameter.IsParsable) continue;

            fastParseCase.AppendLine($"                    case \"--{parameter.Name}\":");
            foreach (var alias in parameter.Aliases)
            {
                fastParseCase.AppendLine($"                    case \"{alias}\":");
            }
            fastParseCase.AppendLine($"                        {parameter.BuildParseMethod(i, parameter.Name, wellKnownTypes)}");
            if (!parameter.HasDefaultValue)
            {
                fastParseCase.AppendLine($"                        arg{i}Parsed = true;");
            }
            fastParseCase.AppendLine("                        break;");
        }

        // parse argument(slow, if ignorecase) ->
        var slowIgnoreCaseParse = new StringBuilder();
        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            if (!parameter.IsParsable) continue;

            slowIgnoreCaseParse.AppendLine($"                        if (string.Equals(name, \"--{parameter.Name}\", StringComparison.OrdinalIgnoreCase){(parameter.Aliases.Length == 0 ? ")" : "")}");
            for (int j = 0; j < parameter.Aliases.Length; j++)
            {
                var alias = parameter.Aliases[j];
                slowIgnoreCaseParse.AppendLine($"                         || string.Equals(name, \"{alias}\", StringComparison.OrdinalIgnoreCase){(parameter.Aliases.Length == j + 1 ? ")" : "")}");
            }
            slowIgnoreCaseParse.AppendLine("                        {");
            slowIgnoreCaseParse.AppendLine($"                            {parameter.BuildParseMethod(i, parameter.Name, wellKnownTypes)}");
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

        // invoke for sync/async, void/int
        var methodArguments = string.Join(", ", command.Parameters.Select((x, i) => $"arg{i}!"));
        var invokeCommand = $"command({methodArguments})";
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

        var invoke = new StringBuilder();
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
        var methodName = isRunAsync ? "RunAsync" : "Run";
        var unsafeCode = (command.MethodKind == MethodKind.FunctionPointer) ? "unsafe " : "";

        var commandMethodType = command.BuildDelegateSignature(out var delegateType);

        var code = $$"""
    public static {{unsafeCode}}{{returnType}} {{methodName}}(string[] args, {{commandMethodType}} command)
    {
{{prepareArgument}}
        try
        {
            for (int i = 0; i < args.Length; i++)
            {
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
{{invoke}}
        catch (Exception ex)
        {
            Environment.ExitCode = 1;
            LogError(ex.ToString());
        }
    }
""";

        if (delegateType != null)
        {
            code += $$"""


    internal {{delegateType}}
""";
        }

        return code;
    }
}
