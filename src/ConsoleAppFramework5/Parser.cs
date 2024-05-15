using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConsoleAppFramework;

internal class Parser(SourceProductionContext context, InvocationExpressionSyntax node, SemanticModel model, WellKnownTypes wellKnownTypes)
{
    public Command? ParseAndValidate()
    {
        var args = node.ArgumentList.Arguments;
        if (args.Count == 2) // 0 = args, 1 = lambda
        {
            var lambda = args[1].Expression as ParenthesizedLambdaExpressionSyntax;
            if (lambda == null)
            {
                if (args[1].Expression.IsKind(SyntaxKind.AddressOfExpression))
                {
                    var operand = (args[1].Expression as PrefixUnaryExpressionSyntax)!.Operand;

                    var methodSymbols = model.GetMemberGroup(operand);
                    if (methodSymbols.Length > 0 && methodSymbols[0] is IMethodSymbol methodSymbol)
                    {
                        return ParseFromMethodSymbol(methodSymbol, addressOf: true);
                    }
                }
                else
                {
                    var methodSymbols = model.GetMemberGroup(args[1].Expression);
                    if (methodSymbols.Length > 0 && methodSymbols[0] is IMethodSymbol methodSymbol)
                    {
                        return ParseFromMethodSymbol(methodSymbol, addressOf: false);
                    }
                }

                return null;
            }
            else
            {
                return ParseFromLambda(lambda);
            }
        }

        return null;
    }

    Command? ParseFromLambda(ParenthesizedLambdaExpressionSyntax lambda)
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
                    // others, invalid.
                    // TODO: validation invalid.
                }
            }
            else
            {
                var firstType = (lambda.ReturnType as GenericNameSyntax)?.TypeArgumentList.Arguments.FirstOrDefault();
                if (firstType == null)
                {
                    isVoid = true; // strictly, should check ret-type is Task...
                }
                else if ((firstType as PredefinedTypeSyntax)?.Keyword.IsKind(SyntaxKind.IntKeyword) ?? false)
                {
                    isVoid = false;
                }
                else
                {
                    // TODO: validation invalid
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
                    HasDefaultValue = hasDefault,
                    DefaultValue = defaultValue,
                    CustomParserType = customParserType,
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
            CommandName = "",
            IsAsync = isAsync,
            IsRootCommand = true,
            IsVoid = isVoid,
            Parameters = parameters,
            MethodKind = MethodKind.Lambda,
            Description = ""
        };

        return cmd;
    }

    Command? ParseFromMethodSymbol(IMethodSymbol methodSymbol, bool addressOf)
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
                // TODO: invalid return
                return null;
            }
        }
        else
        {
            // TODO: invalid return type
            return null;
        }

        var parsableIndex = 0;
        var parameters = methodSymbol.Parameters
            .Select(x =>
            {
                var customParserType = x.GetAttributes().FirstOrDefault(x => x.AttributeClass?.AllInterfaces.Any(y => y.Name == "IArgumentParser") ?? false);
                var hasFromServices = x.GetAttributes().Any(x => x.AttributeClass?.Name == "FromServicesAttribute");
                var hasArgument = x.GetAttributes().Any(x => x.AttributeClass?.Name == "ArgumentAttribute");
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
                    Type = x.Type,
                    HasDefaultValue = x.HasExplicitDefaultValue,
                    DefaultValue = x.HasExplicitDefaultValue ? x.ExplicitDefaultValue : null,
                    CustomParserType = null,
                    IsCancellationToken = isCancellationToken,
                    IsFromServices = hasFromServices,
                    Aliases = aliases,
                    ArgumentIndex = argumentIndex,
                    Description = description
                };
            })
            .ToArray();

        var cmd = new Command
        {
            CommandName = "",
            IsAsync = isAsync,
            IsRootCommand = true,
            IsVoid = isVoid,
            Parameters = parameters,
            MethodKind = addressOf ? MethodKind.FunctionPointer : MethodKind.Method,
            Description = summary
        };

        return cmd;
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
