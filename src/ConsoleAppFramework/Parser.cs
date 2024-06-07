using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConsoleAppFramework;

internal class Parser(SourceProductionContext context, InvocationExpressionSyntax node, SemanticModel model, WellKnownTypes wellKnownTypes, DelegateBuildType delegateBuildType, FilterInfo[] globalFilters)
{
    public Command? ParseAndValidateForRun() // for ConsoleApp.Run, lambda or method or &method
    {
        var args = node.ArgumentList.Arguments;
        if (args.Count == 2) // 0 = args, 1 = lambda
        {
            var command = ExpressionToCommand(args[1].Expression, ""); // rootCommand(commandName = "")
            if (command != null)
            {
                return ValidateCommand(command);
            }
            return null;
        }

        context.ReportDiagnostic(DiagnosticDescriptors.RequireArgsAndMethod, node.GetLocation());
        return null;
    }

    public Command? ParseAndValidateForBuilderDelegateRegistration() // for ConsoleAppBuilder.Add
    {
        // Add(string commandName, Delgate command)
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

        return null;
    }

    public Command?[] ParseAndValidateForBuilderClassRegistration()
    {
        // Add<T>
        var genericName = (node.Expression as MemberAccessExpressionSyntax)?.Name as GenericNameSyntax;
        var genericType = genericName!.TypeArgumentList.Arguments[0];

        // Add<T>(string commandPath)
        string? commandPath = null;
        var args = node.ArgumentList.Arguments;
        if (node.ArgumentList.Arguments.Count == 1)
        {
            var commandName = args[0];
            if (!commandName.Expression.IsKind(SyntaxKind.StringLiteralExpression))
            {
                context.ReportDiagnostic(DiagnosticDescriptors.AddCommandMustBeStringLiteral, commandName.GetLocation());
                return [];
            }

            commandPath = (commandName.Expression as LiteralExpressionSyntax)!.Token.ValueText;
        }

        // T
        var type = model.GetTypeInfo(genericType).Type!;

        if (type.IsStatic || type.IsAbstract)
        {
            context.ReportDiagnostic(DiagnosticDescriptors.ClassIsStaticOrAbstract, node.GetLocation());
            return [];
        }

        var publicMethods = type.GetMembers()
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .OfType<IMethodSymbol>()
            .Where(x => x.DeclaredAccessibility == Accessibility.Public && !x.IsStatic)
            .Where(x => x.MethodKind == Microsoft.CodeAnalysis.MethodKind.Ordinary)
            .Where(x => !(x.Name is "Dispose" or "DisposeAsync" or "GetHashCode" or "Equals" or "ToString"))
            .ToArray();

        var publicConstructors = type.GetMembers()
           .OfType<IMethodSymbol>()
           .Where(x => x.MethodKind == Microsoft.CodeAnalysis.MethodKind.Constructor && x.DeclaredAccessibility == Accessibility.Public)
           .ToArray();

        if (publicMethods.Length == 0)
        {
            context.ReportDiagnostic(DiagnosticDescriptors.ClassHasNoPublicMethods, node.GetLocation());
            return [];
        }

        if (publicConstructors.Length != 1)
        {
            context.ReportDiagnostic(DiagnosticDescriptors.ClassMultipleConsturtor, node.GetLocation());
            return [];
        }

        var hasIDisposable = type.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, wellKnownTypes.IDisposable));
        var hasIAsyncDisposable = type.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, wellKnownTypes.IAsyncDisposable));

        var typeFilters = type.GetAttributes()
            .Where(x => x.AttributeClass?.Name == "ConsoleAppFilterAttribute")
            .Select(x =>
            {
                var filterType = x.AttributeClass!.TypeArguments[0];
                var filter = FilterInfo.Create(filterType);

                if (filter == null)
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.FilterMultipleConsturtor, x.ApplicationSyntaxReference!.GetSyntax().GetLocation());
                    return null!;
                }

                return filter;
            })
            .ToArray();
        if (typeFilters.Any(x => x == null))
        {
            return [];
        }

        var methodInfoBase = new CommandMethodInfo
        {
            TypeFullName = type.ToFullyQualifiedFormatDisplayString(),
            IsIDisposable = hasIDisposable,
            IsIAsyncDisposable = hasIAsyncDisposable,
            ConstructorParameterTypes = publicConstructors[0].Parameters.Select(x => x.Type).ToArray(),
            MethodName = "", // without methodname
        };

        return publicMethods
            .Select(x =>
            {
                string commandName;
                var commandAttribute = x.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "CommandAttribute");
                if (commandAttribute != null)
                {
                    commandName = (x.GetAttributes()[0].ConstructorArguments[0].Value as string)!;
                }
                else
                {
                    commandName = NameConverter.ToKebabCase(x.Name);
                }

                var command = ParseFromMethodSymbol(x, false, (commandPath == null) ? commandName : $"{commandPath.Trim()} {commandName}", typeFilters);
                if (command == null) return null;

                command.CommandMethodInfo = methodInfoBase with { MethodName = x.Name };
                return command;
            })
            .ToArray();
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
                    return ParseFromMethodSymbol(methodSymbol, addressOf: true, commandName, []);
                }
            }
            else
            {
                var methodSymbols = model.GetMemberGroup(expression);
                if (methodSymbols.Length > 0 && methodSymbols[0] is IMethodSymbol methodSymbol)
                {
                    return ParseFromMethodSymbol(methodSymbol, addressOf: false, commandName, []);
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
                else if (x.Default != null)
                {
                    var value = model.GetConstantValue(x.Default.Value);
                    if (value.HasValue)
                    {
                        defaultValue = value.Value;
                    }
                }

                var hasParams = x.Modifiers.Any(x => x.IsKind(SyntaxKind.ParamsKeyword));

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
                var isConsoleAppContext = type.Type!.Name == "ConsoleAppContext";

                var argumentIndex = -1;
                if (!(isFromServices || isCancellationToken || isConsoleAppContext))
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
                    Name = NameConverter.ToKebabCase(x.Identifier.Text),
                    OriginalParameterName = x.Identifier.Text,
                    IsNullableReference = isNullableReference,
                    IsConsoleAppContext = isConsoleAppContext,
                    IsParams = hasParams,
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
            Name = commandName,
            IsAsync = isAsync,
            IsVoid = isVoid,
            Parameters = parameters,
            MethodKind = MethodKind.Lambda,
            Description = "",
            DelegateBuildType = delegateBuildType,
            Filters = globalFilters,
        };

        return cmd;
    }

    Command? ParseFromMethodSymbol(IMethodSymbol methodSymbol, bool addressOf, string commandName, FilterInfo[] typeFilters)
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
                var syntax = methodSymbol.DeclaringSyntaxReferences[0].GetSyntax();
                var location = syntax switch
                {
                    MethodDeclarationSyntax x => x.ReturnType.GetLocation(),
                    LocalFunctionStatementSyntax x => x.ReturnType.GetLocation(),
                    _ => node.GetLocation()
                };

                context.ReportDiagnostic(DiagnosticDescriptors.ReturnTypeMethod, location, methodSymbol.ReturnType);
                return null;
            }
        }
        else
        {
            var syntax = methodSymbol.DeclaringSyntaxReferences[0].GetSyntax();
            var location = syntax switch
            {
                MethodDeclarationSyntax x => x.ReturnType.GetLocation(),
                LocalFunctionStatementSyntax x => x.ReturnType.GetLocation(),
                _ => node.GetLocation()
            };

            context.ReportDiagnostic(DiagnosticDescriptors.ReturnTypeMethod, location, methodSymbol.ReturnType);
            return null;
        }

        var methodFilters = methodSymbol.GetAttributes()
            .Where(x => x.AttributeClass?.Name == "ConsoleAppFilterAttribute")
            .Select(x =>
            {
                var filterType = x.AttributeClass!.TypeArguments[0];
                var filter = FilterInfo.Create(filterType);

                if (filter == null)
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.FilterMultipleConsturtor, x.ApplicationSyntaxReference!.GetSyntax().GetLocation());
                    return null!;
                }

                return filter;
            })
            .ToArray();
        if (methodFilters.Any(x => x == null))
        {
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
                var isConsoleAppContext = x.Type!.Name == "ConsoleAppContext";

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
                    Name = NameConverter.ToKebabCase(x.Name),
                    OriginalParameterName = x.Name,
                    IsNullableReference = isNullableReference,
                    IsConsoleAppContext = isConsoleAppContext,
                    IsParams = x.IsParams,
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
            Name = commandName,
            IsAsync = isAsync,
            IsVoid = isVoid,
            Parameters = parameters,
            MethodKind = addressOf ? MethodKind.FunctionPointer : MethodKind.Method,
            Description = summary,
            DelegateBuildType = delegateBuildType,
            Filters = globalFilters.Concat(typeFilters).Concat(methodFilters).ToArray(),
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
