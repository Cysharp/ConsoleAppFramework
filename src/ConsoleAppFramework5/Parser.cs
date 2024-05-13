using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConsoleAppFramework;

internal class Parser(SourceProductionContext context, InvocationExpressionSyntax node, SemanticModel model)
{
    public Command? ParseAndValidate()
    {
        var args = node.ArgumentList.Arguments;
        if (args.Count == 2) // 0 = args, 1 = lambda
        {
            var lambda = args[1].Expression as ParenthesizedLambdaExpressionSyntax;
            if (lambda == null)
            {
                // TODO: validation(ReportDiagnostic)
                return null;
            }

            if (!lambda.Modifiers.Any(x => x.IsKind(SyntaxKind.StaticKeyword)))
            {
                // TODO: validation(need static)
                return null;
            }

            // TODO: check return type

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
                        defaultValue = token.Value;
                    }

                    // bool is always optional flag
                    if (type.Type?.SpecialType == SpecialType.System_Boolean)
                    {
                        hasDefault = true;
                        defaultValue = false;
                    }

                    var commandAttr = x.AttributeLists.SelectMany(x => x.Attributes)
                      .FirstOrDefault(x =>
                      {
                          var name = x.Name;
                          if (x.Name is QualifiedNameSyntax qns)
                          {
                              name = qns.Right;
                          }

                          var identifier = (name as GenericNameSyntax)?.Identifier;
                          return identifier?.ValueText is "Parser" or "ParserAttribute";
                      });

                    ITypeSymbol? customParserType = null;
                    if (commandAttr != null)
                    {
                        var name = commandAttr.Name;
                        if (commandAttr.Name is QualifiedNameSyntax qns)
                        {
                            name = qns.Right;
                        }
                        var parserType = (name as GenericNameSyntax)?.TypeArgumentList.Arguments[0];
                        if (parserType != null)
                        {
                            customParserType = model.GetTypeInfo(parserType).Type;
                        }
                        // TODO: validation, Type is IParsable?
                    }

                    return new CommandParameter
                    {
                        Name = x.Identifier.Text,
                        Type = type.Type!,
                        HasDefaultValue = hasDefault,
                        DefaultValue = defaultValue,
                        CustomParserType = customParserType,
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
                Parameters = parameters
            };

            return cmd;
        }

        return null;
    }
}
