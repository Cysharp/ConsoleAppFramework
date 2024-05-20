using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection.Metadata;

namespace ConsoleAppFramework;

internal class Parser(SourceProductionContext context, InvocationExpressionSyntax node, SemanticModel model, WellKnownTypes wellKnownTypes, bool disableBuildDefaultValueDelgate)
{
    public Command? ParseAndValidate() // for ConsoleApp.Run
    {
        var args = node.ArgumentList.Arguments;
        if (args.Count == 2) // 0 = args, 1 = lambda
        {
            var command = ExpressionToCommand(args[1].Expression, ""); // rootCommand = commandName = ""
            if (command != null)
            {
                return ValidateCommand(command);
            }
            return null;
        }

        context.ReportDiagnostic(DiagnosticDescriptors.RequireArgsAndMethod, node.GetLocation());
        return null;
    }

    public Command? ParseAndValidateForCommand() // for ConsoleAppBuilder.Add
    {
        var args = node.ArgumentList.Arguments;
        if (args.Count == 2) // 0 = string command, 1 = lambda
        {
            var commandName = args[0];

            if (!commandName.Expression.IsKind(SyntaxKind.StringLiteralExpression))
            {
                context.ReportDiagnostic(DiagnosticDescriptors.AddCommandMustBeStringLiteral, commandName.GetLocation());
                return null;
            }

            var name = (commandName.Expression as LiteralExpressionSyntax)!.Token.ValueText;
            var command = ExpressionToCommand(args[1].Expression, name);
            if (command != null)
            {
                return ValidateCommand(command);
            }
            return null;
        }

        context.ReportDiagnostic(DiagnosticDescriptors.RequireArgsAndMethod, node.GetLocation());
        return null;
    }

    Command? ExpressionToCommand(ExpressionSyntax expression, string commandName)
    {
        var lambda = expression as ParenthesizedLambdaExpressionSyntax;
        if (lambda == null)
        {
            if (expression.IsKind(SyntaxKind.AddressOfExpression))
            {
                var operand = (expression as PrefixUnaryExpressionSyntax)!.Operand;

                var methodSymbols = model.GetMemberGroup(operand);
                if (methodSymbols.Length > 0 && methodSymbols[0] is IMethodSymbol methodSymbol)
                {
                    return ParseFromMethodSymbol(methodSymbol, addressOf: true, commandName);
                }
            }
            else
            {
                var methodSymbols = model.GetMemberGroup(expression);
                if (methodSymbols.Length > 0 && methodSymbols[0] is IMethodSymbol methodSymbol)
                {
                    return ParseFromMethodSymbol(methodSymbol, addressOf: false, commandName);
                }
            }
        }
        else
        {
            return ParseFromLambda(lambda, commandName);
        }

        return null;
    }

    Command? ParseFromLambda(ParenthesizedLambdaExpressionSyntax lambda, string commandName)
    {
        var isAsync = lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);

        var isVoid = lambda.ReturnType == null;
        if (!isVoid)
        {
            if (!isAsync)
            {
                var keyword = (lambda.ReturnType as PredefinedTypeSyntax)?.Keyword;
                if (keyword != null && keyword.Value.IsKind(SyntaxKind.VoidKeyword))
                {
                    isVoid = true;
                }
                else if (keyword != null && keyword.Value.IsKind(SyntaxKind.IntKeyword))
                {
                    isVoid = false; // ok
                }
                else
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.ReturnTypeLambda, lambda.ReturnType!.GetLocation(), lambda.ReturnType);
                    return null;
                }
            }
            else
            {
                if (!(lambda.ReturnType?.ToString() is "Task" or "Task<int>"))
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.ReturnTypeLambda, lambda.ReturnType!.GetLocation(), lambda.ReturnType);
                    return null;
                }

                var firstType = (lambda.ReturnType as GenericNameSyntax)?.TypeArgumentList.Arguments.FirstOrDefault();
                if (firstType == null)
                {
                    isVoid = true;
                }
                else if ((firstType as PredefinedTypeSyntax)?.Keyword.IsKind(SyntaxKind.IntKeyword) ?? false)
                {
                    isVoid = false;
                }
                else
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.ReturnTypeLambda, lambda.ReturnType!.GetLocation(), lambda.ReturnType);
                    return null;
                }
            }
        }

        var parsableIndex = 0;
        var parameters = lambda.ParameterList.Parameters
            .Where(x => x.Type != null)
            .Select(x =>
            {
                var type = model.GetTypeInfo(x.Type!);

                var hasDefault = x.Default != null;
                object? defaultValue = null;
                if (x.Default?.Value is LiteralExpressionSyntax literal)
                {
                    var token = literal.Token;
                    if (token.IsKind(SyntaxKind.DefaultKeyword))
                    {
                        defaultValue = null;
                    }
                    else
                    {
                        defaultValue = token.Value;
                    }
                }

                // bool is always optional flag
                if (type.Type?.SpecialType == SpecialType.System_Boolean)
                {
                    hasDefault = true;
                    defaultValue = false;
                }

                var customParserType = x.AttributeLists.SelectMany(x => x.Attributes)
                    .Select(x =>
                    {
                        var attr = model.GetTypeInfo(x).Type;
                        if (attr != null && attr.AllInterfaces.Any(x => x.Name == "IArgumentParser"))
                        {
                            return attr;
                        }
                        return null;
                    })
                    .FirstOrDefault(x => x != null);

                var hasValidation = x.AttributeLists.SelectMany(x => x.Attributes)
                    .Any(x =>
                    {
                        var attr = model.GetTypeInfo(x).Type as INamedTypeSymbol;
                        if (attr != null && attr.GetBaseTypes().Any(x => x.Name == "ValidationAttribute"))
                        {
                            return true;
                        }
                        return false;
                    });

                var isFromServices = x.AttributeLists.SelectMany(x => x.Attributes)
                  .Any(x =>
                  {
                      var name = x.Name;
                      if (x.Name is QualifiedNameSyntax qns)
                      {
                          name = qns.Right;
                      }

                      var identifier = name.ToString();
                      return identifier is "FromServices" or "FromServicesAttribute";
                  });

                var hasArgument = x.AttributeLists.SelectMany(x => x.Attributes)
                  .Any(x =>
                  {
                      var name = x.Name;
                      if (x.Name is QualifiedNameSyntax qns)
                      {
                          name = qns.Right;
                      }

                      var identifier = name.ToString();
                      return identifier is "Argument" or "ArgumentAttribute";
                  });

                var isCancellationToken = SymbolEqualityComparer.Default.Equals(type.Type!, wellKnownTypes.CancellationToken);

                var argumentIndex = -1;
                if (!(isFromServices || isCancellationToken))
                {
                    if (hasArgument)
                    {
                        argumentIndex = parsableIndex++;
                    }
                    else
                    {
                        parsableIndex++;
                    }
                }

                var isNullableReference = x.Type.IsKind(SyntaxKind.NullableType) && type.Type?.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T;

                return new CommandParameter
                {
                    Name = x.Identifier.Text,
                    IsNullableReference = isNullableReference,
                    Type = type.Type!,
                    Location = x.GetLocation(),
                    HasDefaultValue = hasDefault,
                    DefaultValue = defaultValue,
                    CustomParserType = customParserType,
                    HasValidation = hasValidation,
                    IsCancellationToken = isCancellationToken,
                    IsFromServices = isFromServices,
                    Aliases = [],
                    Description = "",
                    ArgumentIndex = argumentIndex,
                };
            })
            .Where(x => x.Type != null)
            .ToArray();

        var cmd = new Command
        {
            CommandName = commandName,
            IsAsync = isAsync,
            IsVoid = isVoid,
            Parameters = parameters,
            MethodKind = MethodKind.Lambda,
            Description = "",
            DisableBuildDefaultValueDelgate = disableBuildDefaultValueDelgate
        };

        return cmd;
    }

    Command? ParseFromMethodSymbol(IMethodSymbol methodSymbol, bool addressOf, string commandName)
    {
        var docComment = methodSymbol.DeclaringSyntaxReferences[0].GetSyntax().GetDocumentationCommentTriviaSyntax();
        var summary = "";
        Dictionary<string, string>? parameterDescriptions = null;
        if (docComment != null)
        {
            summary = docComment.GetSummary();
            parameterDescriptions = docComment.GetParams().ToDictionary(x => x.Name, x => x.Description);
        }

        // allow returnType = void, int, Task, Task<int>
        var isVoid = false;
        var isAsync = false;
        if (methodSymbol.ReturnType.SpecialType == SpecialType.System_Void)
        {
            isVoid = true;
        }
        else if (methodSymbol.ReturnType.SpecialType == SpecialType.System_Int32)
        {
            isVoid = false;
        }
        else if ((methodSymbol.ReturnType as INamedTypeSymbol)!.EqualsUnconstructedGenericType(wellKnownTypes.Task))
        {
            isVoid = true;
            isAsync = true;
        }
        else if ((methodSymbol.ReturnType as INamedTypeSymbol)!.EqualsUnconstructedGenericType(wellKnownTypes.Task_T))
        {
            var typeArg = (methodSymbol.ReturnType as INamedTypeSymbol)!.TypeArguments[0];
            if (typeArg.SpecialType == SpecialType.System_Int32)
            {
                isVoid = false;
                isAsync = true;
            }
            else
            {
                context.ReportDiagnostic(DiagnosticDescriptors.ReturnTypeMethod, node.ArgumentList.Arguments[1].GetLocation(), methodSymbol.ReturnType);
                return null;
            }
        }
        else
        {
            context.ReportDiagnostic(DiagnosticDescriptors.ReturnTypeMethod, node.ArgumentList.Arguments[1].GetLocation(), methodSymbol.ReturnType);
            return null;
        }

        var parsableIndex = 0;
        var parameters = methodSymbol.Parameters
            .Select(x =>
            {
                var customParserType = x.GetAttributes().FirstOrDefault(x => x.AttributeClass?.AllInterfaces.Any(y => y.Name == "IArgumentParser") ?? false);
                var hasFromServices = x.GetAttributes().Any(x => x.AttributeClass?.Name == "FromServicesAttribute");
                var hasArgument = x.GetAttributes().Any(x => x.AttributeClass?.Name == "ArgumentAttribute");
                var hasValidation = x.GetAttributes().Any(x => x.AttributeClass?.GetBaseTypes().Any(y => y.Name == "ValidationAttribute") ?? false);
                var isCancellationToken = SymbolEqualityComparer.Default.Equals(x.Type, wellKnownTypes.CancellationToken);

                string description = "";
                string[] aliases = [];
                if (parameterDescriptions != null && parameterDescriptions.TryGetValue(x.Name, out var desc))
                {
                    ParseParameterDescription(desc, out aliases, out description);
                }

                var argumentIndex = -1;
                if (!(hasFromServices || isCancellationToken))
                {
                    if (hasArgument)
                    {
                        argumentIndex = parsableIndex++;
                    }
                    else
                    {
                        parsableIndex++;
                    }
                }

                var isNullableReference = x.NullableAnnotation == NullableAnnotation.Annotated && x.Type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T;

                return new CommandParameter
                {
                    Name = x.Name,
                    IsNullableReference = isNullableReference,
                    Location = x.DeclaringSyntaxReferences[0].GetSyntax().GetLocation(),
                    Type = x.Type,
                    HasDefaultValue = x.HasExplicitDefaultValue,
                    DefaultValue = x.HasExplicitDefaultValue ? x.ExplicitDefaultValue : null,
                    CustomParserType = null,
                    IsCancellationToken = isCancellationToken,
                    IsFromServices = hasFromServices,
                    HasValidation = hasValidation,
                    Aliases = aliases,
                    ArgumentIndex = argumentIndex,
                    Description = description
                };
            })
            .ToArray();

        var cmd = new Command
        {
            CommandName = commandName,
            IsAsync = isAsync,
            IsVoid = isVoid,
            Parameters = parameters,
            MethodKind = addressOf ? MethodKind.FunctionPointer : MethodKind.Method,
            Description = summary,
            DisableBuildDefaultValueDelgate = disableBuildDefaultValueDelgate
        };

        return cmd;
    }

    Command? ValidateCommand(Command command)
    {
        var hasDiagnostic = false;

        // Sequential Argument
        var existsNotArgument = false;
        foreach (var parameter in command.Parameters)
        {
            if (!parameter.IsParsable) continue;

            if (!parameter.IsArgument)
            {
                existsNotArgument = true;
            }

            if (parameter.IsArgument && existsNotArgument)
            {
                context.ReportDiagnostic(DiagnosticDescriptors.SequentialArgument, parameter.Location);
                hasDiagnostic = true;
            }
        }

        // FunctionPointer can not use validation
        if (command.MethodKind == MethodKind.FunctionPointer)
        {
            foreach (var p in command.Parameters)
            {
                if (p.HasValidation)
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.FunctionPointerCanNotHaveValidation, p.Location);
                    hasDiagnostic = true;
                }
            }
        }

        if (hasDiagnostic) return null;

        return command;
    }

    void ParseParameterDescription(string originalDescription, out string[] aliases, out string description)
    {
        // Example:
        // -h|--help, This is a help.

        var splitOne = originalDescription.Split(',');

        // has alias
        if (splitOne[0].TrimStart().StartsWith("-"))
        {
            aliases = splitOne[0].Split(['|'], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            description = string.Join("", splitOne.Skip(1)).Trim();
        }
        else
        {
            aliases = [];
            description = originalDescription;
        }
    }
}
