using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Net.Sockets;
using System.Reflection.Metadata;

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
        // allow not static...
        //if (!lambda.Modifiers.Any(x => x.IsKind(SyntaxKind.StaticKeyword)))
        //{
        //    // TODO: validation(need static)
        //    return null;
        //}

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

                var parserAttr = x.AttributeLists.SelectMany(x => x.Attributes)
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

                ITypeSymbol? customParserType = null;
                if (parserAttr != null)
                {
                    var name = parserAttr.Name;
                    if (parserAttr.Name is QualifiedNameSyntax qns)
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

                var isCancellationToken = SymbolEqualityComparer.Default.Equals(type.Type!, wellKnownTypes.CancellationToken);

                return new CommandParameter
                {
                    Name = x.Identifier.Text,
                    Type = type.Type!,
                    HasDefaultValue = hasDefault,
                    DefaultValue = defaultValue,
                    CustomParserType = customParserType,
                    IsCancellationToken = isCancellationToken,
                    IsFromServices = isFromServices,
                    Aliases = [],
                    Description = ""
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

        // TODO: check for Task, Task<int>

        var parameters = methodSymbol.Parameters
            .Select(x =>
            {
                var parserAttr = x.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "ParserAttribute");

                if (parserAttr != null)
                {
                    // TODO: get parser
                }

                // TODO: check FromServcies Attribute

                var isCancellationToken = SymbolEqualityComparer.Default.Equals(x.Type, wellKnownTypes.CancellationToken);

                string description = "";
                string[] aliases = [];
                if (parameterDescriptions != null && parameterDescriptions.TryGetValue(x.Name, out var desc))
                {
                    ParseParameterDescription(desc, out aliases, out description);
                }

                return new CommandParameter
                {
                    Name = x.Name,
                    Type = x.Type,
                    HasDefaultValue = x.HasExplicitDefaultValue,
                    DefaultValue = x.HasExplicitDefaultValue ? x.ExplicitDefaultValue : null,
                    CustomParserType = null,
                    IsCancellationToken = isCancellationToken,
                    IsFromServices = false,
                    Aliases = aliases,
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
