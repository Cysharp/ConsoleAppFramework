using Microsoft.CodeAnalysis;
using System.Text;

namespace ConsoleAppFramework;

internal class Emitter(SourceProductionContext context, Command command, WellKnownTypes wellKnownTypes)
{
    public string Emit()
    {
        // prepare argument ->
        // parse argument ->
        // validate parsed ->
        // execute

        var prepareArgument = new StringBuilder();
        for (var i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            var defaultValue = parameter.HasDefaultValue ? parameter.DefaultValueToString() : $"default({parameter.Type.ToFullyQualifiedFormatDisplayString()})";
            prepareArgument.AppendLine($"        var arg{i} = {defaultValue};");
            if (!parameter.HasDefaultValue)
            {
                prepareArgument.AppendLine($"        var arg{i}Parsed = false;");
            }
        }

        var fastParseCase = new StringBuilder();
        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            fastParseCase.AppendLine($"                case \"--{parameter.Name}\":");
            fastParseCase.AppendLine($"                    {parameter.BuildParseMethod(i, parameter.Name, wellKnownTypes)}");
            if (!parameter.HasDefaultValue)
            {
                fastParseCase.AppendLine($"                    arg{i}Parsed = true;");
            }
            fastParseCase.AppendLine("                    break;");
        }

        var slowIgnoreCaseParse = new StringBuilder();
        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
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

        var validateParsed = new StringBuilder();
        for (int i = 0; i < command.Parameters.Length; i++)
        {
            var parameter = command.Parameters[i];
            if (!parameter.HasDefaultValue)
            {
                validateParsed.AppendLine($"        if (!arg{i}Parsed) ThrowRequiredArgumentNotParsed(\"{parameter.Name}\");");
            }
        }

        var methodArguments = string.Join(", ", command.Parameters.Select((x, i) => $"arg{i}!"));

        // TODO: Run or RunAsync
        // TODO: void or int and handle it.
        // isASync, need GetAwaiter().GetResult();
        var code = $$"""
    public static void Run(string[] args, {{command.BuildDelegateSignature()}} command)
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
                    ThrowInvalidArgumentName(name);
                    break;
            }
        }

{{validateParsed}}
        command({{methodArguments}});
    }
""";

        return code;
    }
}
