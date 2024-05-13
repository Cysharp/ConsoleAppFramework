using Microsoft.CodeAnalysis;
using System.Text;

namespace ConsoleAppFramework;

internal class Emitter(SourceProductionContext context, Command command, WellKnownTypes wellKnownTypes)
{
    public string EmitRun(bool isRunAsync)
    {
        var hasCancellationToken = command.Parameters.Any(x => x.IsCancellationToken);

        // prepare argument variables ->
        var prepareArgument = new StringBuilder();
        if (hasCancellationToken)
        {
            prepareArgument.AppendLine("        using var posixSignalHandler = PosixSignalHandler.Register();");
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

            fastParseCase.AppendLine($"                case \"--{parameter.Name}\":");
            fastParseCase.AppendLine($"                    {parameter.BuildParseMethod(i, parameter.Name, wellKnownTypes)}");
            if (!parameter.HasDefaultValue)
            {
                fastParseCase.AppendLine($"                    arg{i}Parsed = true;");
            }
            fastParseCase.AppendLine("                    break;");
        }

        // parse argument(slow, if ignorecase) ->
        var slowIgnoreCaseParse = new StringBuilder();
        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            if (!parameter.IsParsable) continue;

            slowIgnoreCaseParse.AppendLine($"                    if (string.Equals(name, \"--{parameter.Name}\", StringComparison.OrdinalIgnoreCase))");
            slowIgnoreCaseParse.AppendLine("                    {");
            slowIgnoreCaseParse.AppendLine($"                        {parameter.BuildParseMethod(i, parameter.Name, wellKnownTypes)}");
            if (!parameter.HasDefaultValue)
            {
                slowIgnoreCaseParse.AppendLine($"                        arg{i}Parsed = true;");
            }
            slowIgnoreCaseParse.AppendLine($"                        break;");
            slowIgnoreCaseParse.AppendLine("                    }");
        }

        // validate parsed ->
        var validateParsed = new StringBuilder();
        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            if (!parameter.IsParsable) continue;

            if (!parameter.HasDefaultValue)
            {
                validateParsed.AppendLine($"        if (!arg{i}Parsed) ThrowRequiredArgumentNotParsed(\"{parameter.Name}\");");
            }
        }

        // invoke for sync/async, void/int
        var methodArguments = string.Join(", ", command.Parameters.Select((x, i) => $"arg{i}!"));
        var invoke = new StringBuilder();
        if (hasCancellationToken)
        {
            invoke.AppendLine("        try");
            invoke.AppendLine("        {");
            invoke.Append("    ");
        }

        if (command.IsAsync)
        {
            if (command.IsVoid)
            {
                if (isRunAsync)
                {
                    invoke.AppendLine($"        await command({methodArguments});");
                }
                else
                {
                    invoke.AppendLine($"        command({methodArguments}).GetAwaiter().GetResult();");
                }
            }
            else
            {
                if (isRunAsync)
                {
                    invoke.AppendLine($"        Environment.ExitCode = await command({methodArguments});");
                }
                else
                {
                    invoke.AppendLine($"        Environment.ExitCode = command({methodArguments}).GetAwaiter().GetResult();");
                }
            }
        }
        else
        {
            if (command.IsVoid)
            {
                invoke.AppendLine($"        command({methodArguments});");
            }
            else
            {
                invoke.AppendLine($"        Environment.ExitCode = command({methodArguments});");
            }
        }

        if (hasCancellationToken)
        {
            invoke.AppendLine("        }");
            invoke.AppendLine("        catch (OperationCanceledException ex) when (ex.CancellationToken == posixSignalHandler.Token) { }");
        }

        var returnType = isRunAsync ? "async Task" : "void";
        var methodName = isRunAsync ? "RunAsync" : "Run";
        var unsafeCode = (command.MethodKind == MethodKind.FunctionPointer) ? "unsafe " : "";

        var commandMethodType = command.BuildDelegateSignature(out var delegateType);

        var code = $$"""
    public static {{unsafeCode}}{{returnType}} {{methodName}}(string[] args, {{commandMethodType}} command)
    {
{{prepareArgument}}
        for (int i = 0; i < args.Length; i++)
        {
            var name = args[i];

            switch (name)
            {
{{fastParseCase}}
                default:
{{slowIgnoreCaseParse}}
                    break;
            }
        }

{{validateParsed}}
{{invoke}}
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
