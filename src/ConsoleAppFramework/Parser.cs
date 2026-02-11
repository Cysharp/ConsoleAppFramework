using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConsoleAppFramework;

internal class Parser(ConsoleAppFrameworkGeneratorOptions generatorOptions, DiagnosticReporter context, SyntaxNode node, SemanticModel model, WellKnownTypes wellKnownTypes, DelegateBuildType delegateBuildType, FilterInfo[] globalFilters)
{
    public Command? ParseAndValidateForRun() // for ConsoleApp.Run, lambda or method or &method
    {
        var args = (node as InvocationExpressionSyntax)!.ArgumentList.Arguments;
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
        // Add(string commandName, Delegate command)
        var args = (node as InvocationExpressionSyntax)!.ArgumentList.Arguments;
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
        var node2 = (node as InvocationExpressionSyntax)!;
        // Add<T>
        var genericName = (node2.Expression as MemberAccessExpressionSyntax)?.Name as GenericNameSyntax;
        var genericType = genericName!.TypeArgumentList.Arguments[0];

        // Add<T>(string commandPath)
        string? commandPath = null;
        var args = node2.ArgumentList.Arguments;
        if (node2.ArgumentList.Arguments.Count == 1)
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

        return CreateCommandsFromType(type, commandPath);
    }

    public Command?[] CreateCommandsFromType(ITypeSymbol type, string? commandPath)
    {
        if (type.IsStatic || type.IsAbstract)
        {
            context.ReportDiagnostic(DiagnosticDescriptors.ClassIsStaticOrAbstract, node.GetLocation());
            return [];
        }

        if (type.DeclaringSyntaxReferences.Length == 0)
        {
            context.ReportDiagnostic(DiagnosticDescriptors.DefinedInOtherProject, node.GetLocation());
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
            context.ReportDiagnostic(DiagnosticDescriptors.ClassMultipleConstructor, node.GetLocation());
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
                    context.ReportDiagnostic(DiagnosticDescriptors.FilterMultipleConstructor, x.ApplicationSyntaxReference!.GetSyntax().GetLocation());
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
            ConstructorParameterTypes = publicConstructors[0].Parameters.Select(x => new EquatableTypeSymbolWithKeyedServiceKey(x)).ToArray(),
            MethodName = "", // without method name
        };

        return publicMethods
            .Select(x =>
            {
                string commandName;
                var commandAttribute = x.GetAttributes().FirstOrDefault(x => x.AttributeClass?.Name == "CommandAttribute");
                if (commandAttribute != null)
                {
                    commandName = (commandAttribute.ConstructorArguments[0].Value as string)!;
                }
                else
                {
                    commandName = generatorOptions.DisableNamingConversion ? x.Name : NameConverter.ToKebabCase(x.Name);
                }

                var command = ParseFromMethodSymbol(x, false, (commandPath == null) ? commandName : $"{commandPath.Trim()} {commandName}", typeFilters);
                if (command == null) return null;

                command.CommandMethodInfo = methodInfoBase with { MethodName = x.Name };
                return command;
            })
            .ToArray();
    }

    public GlobalOptionInfo[] ParseGlobalOptions()
    {
        if (node is not InvocationExpressionSyntax lambdaExpr) return [];

        var addOptions = node
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(x =>
            {
                var expr = x.Expression as MemberAccessExpressionSyntax;
                var methodName = expr?.Name.Identifier.Text;
                if (methodName is "AddGlobalOption")
                {
                    return new { node = x, expr, required = false };
                }
                else if (methodName is "AddRequiredGlobalOption")
                {
                    return new { node = x, expr, required = true };
                }

                return null;
            })
            .Where(x => x != null);

        var result = addOptions
            .Select(x =>
            {
                var node = x!.node;
                var memberAccess = x.expr!;
                var symbolInfo = model.GetSymbolInfo(memberAccess).Symbol as IMethodSymbol;

                var typeSymbol = symbolInfo!.TypeArguments[0];

                if (!IsParsableType(typeSymbol))
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.InvalidGlobalOptionsType, node.GetLocation());
                    return null!;
                }

                object? name = "";
                object? description = "";
                object? defaultValue = GetDefaultValue(typeSymbol);

                var arguments = node.ArgumentList;

                if (!x.required)
                {
                    // public T AddGlobalOption<T>([ConstantExpected] string name, [ConstantExpected] string description = "", [ConstantExpected] T defaultValue = default(T))
                    node.ArgumentList.GetConstantValues(model, "name", "description", "defaultValue", ref name, ref description, ref defaultValue);
                }
                else
                {
                    // public T AddRequiredGlobalOption<T>([ConstantExpected] string name, [ConstantExpected] string description = "")
                    node.ArgumentList.GetConstantValues(model, "name", "description", ref name, ref description);
                }

                return new GlobalOptionInfo
                {
                    Type = new(typeSymbol),
                    IsRequired = x.required,
                    Name = (string)name!,
                    Description = (string)description!,
                    DefaultValue = defaultValue
                };
            })
            .Where(x => x != null)
            .ToArray();

        return result;

        // GlobalOptions allow type is limited(C# compile-time constant only)
        // bool, char, sbyte, byte, short, ushort, int, uint, long, ulong, float, double, decimal
        // string
        // null
        // enum
        // Nullable<T>
        bool IsParsableType(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol { IsValueType: true, OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } namedType)
            {
                var underlyingType = namedType.TypeArguments[0];
                return IsParsableType(underlyingType);
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                case SpecialType.System_String:
                    return true;
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                return true;
            }

            return false;
        }

        object? GetDefaultValue(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol { IsValueType: true, OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } namedType)
            {
                return null;
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean: return false;
                case SpecialType.System_Char: return '\0';
                case SpecialType.System_SByte: return (sbyte)0;
                case SpecialType.System_Byte: return (byte)0;
                case SpecialType.System_Int16: return (short)0;
                case SpecialType.System_UInt16: return (ushort)0;
                case SpecialType.System_Int32: return 0;
                case SpecialType.System_UInt32: return 0u;
                case SpecialType.System_Int64: return 0L;
                case SpecialType.System_UInt64: return 0UL;
                case SpecialType.System_Single: return 0f;
                case SpecialType.System_Double: return 0d;
                case SpecialType.System_Decimal: return 0m;
                case SpecialType.System_String: return null;
                default:
                    break;
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                var enumType = (INamedTypeSymbol)type;
                var underlyingType = enumType.EnumUnderlyingType;
                return underlyingType?.SpecialType switch
                {
                    SpecialType.System_Byte => (byte)0,
                    SpecialType.System_SByte => (sbyte)0,
                    SpecialType.System_Int16 => (short)0,
                    SpecialType.System_UInt16 => (ushort)0,
                    SpecialType.System_Int32 => 0,
                    SpecialType.System_UInt32 => 0u,
                    SpecialType.System_Int64 => 0L,
                    SpecialType.System_UInt64 => 0UL,
                    _ => 0
                };
            }

            return null;
        }
    }

    Command? ExpressionToCommand(ExpressionSyntax expression, string commandName)
    {
        if (expression is ParenthesizedLambdaExpressionSyntax lambda)
        {
            return ParseFromLambda(lambda, commandName);
        }
        else
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
                    var cmd = ParseFromMethodSymbol(methodSymbol, addressOf: false, commandName, []);
                    if (cmd != null)
                    {
                        cmd.IsRequireDynamicDependencyAttribute = true;
                    }
                    return cmd;
                }
            }
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

        var parameterSymbols = lambda.ParameterList.Parameters
            .Where(x => x.Type != null)
            .Select(x => model.GetDeclaredSymbol(x))
            .OfType<IParameterSymbol>()
            .ToImmutableArray();

        if (!TryBuildRuntimeAndParseParameters(parameterSymbols, null, out var parameters, out var effectiveParseParameters, out var asParametersExpansionBindings))
        {
            return null;
        }

        var cmd = new Command
        {
            Name = commandName,
            IsAsync = isAsync,
            IsVoid = isVoid,
            IsHidden = false, // Anonymous lambda don't support attribute.
            Parameters = parameters,
            EffectiveParseParameters = effectiveParseParameters,
            AsParametersExpansionBindings = asParametersExpansionBindings,
            MethodKind = MethodKind.Lambda,
            Description = "",
            DelegateBuildType = delegateBuildType,
            Filters = globalFilters,
            IsRequireDynamicDependencyAttribute = false,
        };

        return cmd;
    }

    Command? ParseFromMethodSymbol(IMethodSymbol methodSymbol, bool addressOf, string commandName, FilterInfo[] typeFilters)
    {
        if (methodSymbol.DeclaringSyntaxReferences.Length == 0)
        {
            context.ReportDiagnostic(DiagnosticDescriptors.DefinedInOtherProject, node.GetLocation());
            return null;
        }

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

            // check `async void`
            if (methodSymbol.DeclaringSyntaxReferences[0].GetSyntax() is MethodDeclarationSyntax syntax)
            {
                var asyncKeyword = syntax.Modifiers.FirstOrDefault(x => x.IsKind(SyntaxKind.AsyncKeyword));
                if (asyncKeyword != default)
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.ReturnTypeMethodAsyncVoid, asyncKeyword.GetLocation());
                    return null;
                }
            }
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

        var isHiddenCommand = methodSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == "HiddenAttribute");

        var methodFilters = methodSymbol.GetAttributes()
            .Where(x => x.AttributeClass?.Name == "ConsoleAppFilterAttribute")
            .Select(x =>
            {
                var filterType = x.AttributeClass!.TypeArguments[0];
                var filter = FilterInfo.Create(filterType);

                if (filter == null)
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.FilterMultipleConstructor, x.ApplicationSyntaxReference!.GetSyntax().GetLocation());
                    return null!;
                }

                return filter;
            })
            .ToArray();
        if (methodFilters.Any(x => x == null))
        {
            return null;
        }

        // validate parameter symbols
        if (parameterDescriptions != null)
        {
            foreach (var item in parameterDescriptions)
            {
                if (!methodSymbol.Parameters.Any(x => x.Name == item.Key))
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.DocCommentParameterNameNotMatched, methodSymbol.Locations[0], item.Key);
                    return null;
                }
            }
        }

        Dictionary<string, ParameterDescription>? parameterDescriptionMetadata = null;
        if (parameterDescriptions != null)
        {
            parameterDescriptionMetadata = parameterDescriptions.ToDictionary(
                static x => x.Key,
                x =>
                {
                    ParseParameterDescription(x.Value, out var aliases, out var description);
                    return new ParameterDescription(aliases, description);
                });
        }

        if (!TryBuildRuntimeAndParseParameters(methodSymbol.Parameters, parameterDescriptionMetadata, out var parameters, out var effectiveParseParameters, out var asParametersExpansionBindings))
        {
            return null;
        }

        var cmd = new Command
        {
            Name = commandName,
            IsAsync = isAsync,
            IsVoid = isVoid,
            IsHidden = isHiddenCommand,
            Parameters = parameters,
            EffectiveParseParameters = effectiveParseParameters,
            AsParametersExpansionBindings = asParametersExpansionBindings,
            MethodKind = addressOf ? MethodKind.FunctionPointer : MethodKind.Method,
            Description = summary,
            DelegateBuildType = delegateBuildType,
            Filters = globalFilters.Concat(typeFilters).Concat(methodFilters).ToArray(),
            Symbol = new IgnoreEquality<ISymbol>(methodSymbol)
        };

        return cmd;
    }

    Command? ValidateCommand(Command command)
    {
        var hasDiagnostic = false;

        // Sequential Argument(v5.7.7 support it).
        //var existsNotArgument = false;
        //foreach (var parameter in command.Parameters)
        //{
        //    if (!parameter.IsParsable) continue;

        //    if (!parameter.IsArgument)
        //    {
        //        existsNotArgument = true;
        //    }

        //    if (parameter.IsArgument && existsNotArgument)
        //    {
        //        context.ReportDiagnostic(DiagnosticDescriptors.SequentialArgument, parameter.Location);
        //        hasDiagnostic = true;
        //    }
        //}

        // FunctionPointer can not use validation
        if (command.MethodKind == MethodKind.FunctionPointer)
        {
            foreach (var p in command.EffectiveParseParameters)
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

    readonly record struct ParameterDescription(EquatableArray<string> Aliases, string Description)
    {
        public static ParameterDescription Empty { get; } = new([], "");
    }

    struct ParameterBuildState
    {
        public int ParsableIndex;
        public int ArgumentIndexCounter;
    }

    bool TryBuildRuntimeAndParseParameters(
        IEnumerable<IParameterSymbol> runtimeParameterSymbols,
        IReadOnlyDictionary<string, ParameterDescription>? runtimeParameterDescriptions,
        out EquatableArray<CommandParameter> runtimeParameters,
        out EquatableArray<CommandParameter> effectiveParseParameters,
        out EquatableArray<AsParametersBinding> asParametersExpansionBindings)
    {
        var runtimeParameterCount = runtimeParameterSymbols switch
        {
            IReadOnlyCollection<IParameterSymbol> readOnlyCollection => readOnlyCollection.Count,
            ICollection<IParameterSymbol> collection => collection.Count,
            _ => -1
        };
        var runtimeParameterBuffer = runtimeParameterCount >= 0 ? new CommandParameter[runtimeParameterCount] : null;
        var runtimeParameterList = runtimeParameterBuffer == null ? new List<CommandParameter>() : null;
        List<CommandParameter>? parseParameterList = null;
        var bindingBuffer = runtimeParameterCount > 0 ? new AsParametersBinding[runtimeParameterCount] : null;
        var bindingCount = 0;
        List<AsParametersBinding>? bindingList = null;

        var runtimeBuildState = new ParameterBuildState();
        var parseBuildState = new ParameterBuildState();

        var runtimeParameterIndex = 0;
        foreach (var runtimeParameterSymbol in runtimeParameterSymbols)
        {
            var parameterDescription = TryGetParameterDescription(runtimeParameterDescriptions, runtimeParameterSymbol.Name);
            var runtimeParameter = BuildCommandParameter(runtimeParameterSymbol, parameterDescription, ref runtimeBuildState);
            if (runtimeParameterBuffer == null)
            {
                runtimeParameterList!.Add(runtimeParameter);
            }
            else
            {
                runtimeParameterBuffer[runtimeParameterIndex] = runtimeParameter;
            }

            if (!HasAttribute(runtimeParameterSymbol, "AsParametersAttribute"))
            {
                if (parseParameterList != null)
                {
                    parseParameterList.Add(BuildEffectiveParseParameter(runtimeParameter, ref parseBuildState));
                }
                else
                {
                    AdvanceEffectiveParseState(runtimeParameter, ref parseBuildState);
                }
                runtimeParameterIndex++;
                continue;
            }

            if (parseParameterList == null)
            {
                parseParameterList = new List<CommandParameter>(runtimeParameterIndex + 4);
                if (runtimeParameterBuffer == null)
                {
                    for (int i = 0; i < runtimeParameterList!.Count - 1; i++)
                    {
                        parseParameterList.Add(runtimeParameterList[i]);
                    }
                }
                else
                {
                    for (int i = 0; i < runtimeParameterIndex; i++)
                    {
                        parseParameterList.Add(runtimeParameterBuffer[i]);
                    }
                }
            }

            if (!TryGetAsParametersConstructor(runtimeParameterSymbol, out var targetType, out var constructor))
            {
                runtimeParameters = [];
                effectiveParseParameters = [];
                asParametersExpansionBindings = [];
                return false;
            }

            var constructorParameterDescriptions = BuildAsParametersConstructorParameterDescriptions(constructor);
            var parseIndexes = new int[constructor.Parameters.Length];
            var parseIndex = 0;
            foreach (var constructorParameter in constructor.Parameters)
            {
                if (HasAttribute(constructorParameter, "AsParametersAttribute"))
                {
                    context.ReportDiagnostic(
                        DiagnosticDescriptors.AsParametersNestedNotSupported,
                        GetParameterLocation(constructorParameter),
                        constructorParameter.Name,
                        targetType.ToDisplayString());
                    runtimeParameters = [];
                    effectiveParseParameters = [];
                    asParametersExpansionBindings = [];
                    return false;
                }

                if (constructorParameter.IsParams)
                {
                    context.ReportDiagnostic(
                        DiagnosticDescriptors.AsParametersParamsNotSupported,
                        GetParameterLocation(constructorParameter),
                        constructorParameter.Name,
                        targetType.ToDisplayString());
                    runtimeParameters = [];
                    effectiveParseParameters = [];
                    asParametersExpansionBindings = [];
                    return false;
                }

                parseIndexes[parseIndex++] = parseParameterList.Count;
                var constructorParameterDescription = TryGetParameterDescription(constructorParameterDescriptions, constructorParameter.Name);
                parseParameterList.Add(BuildCommandParameter(constructorParameter, constructorParameterDescription, ref parseBuildState));
            }

            var binding = new AsParametersBinding
            {
                RuntimeParameterIndex = runtimeParameterIndex,
                TargetType = new EquatableTypeSymbol(targetType),
                ParseParameterIndexes = parseIndexes
            };
            if (bindingBuffer == null)
            {
                (bindingList ??= new List<AsParametersBinding>(1)).Add(binding);
            }
            else
            {
                bindingBuffer[bindingCount++] = binding;
            }

            runtimeParameterIndex++;
        }

        runtimeParameters = runtimeParameterBuffer ?? runtimeParameterList!.ToArray();
        effectiveParseParameters = parseParameterList == null ? runtimeParameters : parseParameterList.ToArray();
        if (bindingBuffer == null)
        {
            asParametersExpansionBindings = bindingList == null ? [] : bindingList.ToArray();
        }
        else if (bindingCount == 0)
        {
            asParametersExpansionBindings = [];
        }
        else if (bindingCount == bindingBuffer.Length)
        {
            asParametersExpansionBindings = bindingBuffer;
        }
        else
        {
            var trimmedBindings = new AsParametersBinding[bindingCount];
            Array.Copy(bindingBuffer, trimmedBindings, bindingCount);
            asParametersExpansionBindings = trimmedBindings;
        }
        return true;
    }

    IReadOnlyDictionary<string, ParameterDescription>? BuildAsParametersConstructorParameterDescriptions(IMethodSymbol constructor)
    {
        if (constructor.DeclaringSyntaxReferences.Length == 0)
        {
            return null;
        }

        var constructorSyntax = constructor.DeclaringSyntaxReferences[0].GetSyntax();
        var docComment = constructorSyntax.GetDocumentationCommentTriviaSyntax();
        if (docComment == null)
        {
            return null;
        }

        var constructorParameterNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var constructorParameter in constructor.Parameters)
        {
            constructorParameterNames.Add(constructorParameter.Name);
        }

        Dictionary<string, ParameterDescription>? parameterDescriptions = null;
        foreach (var (Name, Description) in docComment.GetParams())
        {
            if (!constructorParameterNames.Contains(Name))
            {
                continue;
            }

            ParseParameterDescription(Description, out var aliases, out var description);
            parameterDescriptions ??= new(StringComparer.Ordinal);
            parameterDescriptions[Name] = new ParameterDescription(aliases, description);
        }

        return parameterDescriptions;
    }

    CommandParameter BuildEffectiveParseParameter(CommandParameter runtimeParameter, ref ParameterBuildState buildState)
    {
        var argumentIndex = -1;
        if (runtimeParameter.IsParsable)
        {
            if (runtimeParameter.IsArgument)
            {
                argumentIndex = buildState.ArgumentIndexCounter++;
            }
            else
            {
                buildState.ParsableIndex++;
            }
        }

        return argumentIndex == runtimeParameter.ArgumentIndex
            ? runtimeParameter
            : runtimeParameter with { ArgumentIndex = argumentIndex };
    }

    void AdvanceEffectiveParseState(CommandParameter runtimeParameter, ref ParameterBuildState buildState)
    {
        if (!runtimeParameter.IsParsable) return;

        if (runtimeParameter.IsArgument)
        {
            buildState.ArgumentIndexCounter++;
        }
        else
        {
            buildState.ParsableIndex++;
        }
    }

    bool TryGetAsParametersConstructor(IParameterSymbol runtimeParameter, out INamedTypeSymbol targetType, out IMethodSymbol constructor)
    {
        targetType = null!;
        constructor = null!;

        if (runtimeParameter.Type is not INamedTypeSymbol namedType || !namedType.IsRecord || namedType.TypeKind != TypeKind.Class)
        {
            context.ReportDiagnostic(
                DiagnosticDescriptors.AsParametersTargetMustBeRecordClass,
                GetParameterLocation(runtimeParameter),
                runtimeParameter.Name,
                runtimeParameter.Type.ToDisplayString());
            return false;
        }

        var publicConstructors = namedType.InstanceConstructors
            .Where(x => x.DeclaredAccessibility == Accessibility.Public)
            .ToArray();

        if (publicConstructors.Length != 1)
        {
            context.ReportDiagnostic(
                DiagnosticDescriptors.AsParametersTargetMustHaveSinglePublicConstructor,
                GetParameterLocation(runtimeParameter),
                namedType.ToDisplayString());
            return false;
        }

        targetType = namedType;
        constructor = publicConstructors[0];
        return true;
    }

    CommandParameter BuildCommandParameter(IParameterSymbol parameterSymbol, ParameterDescription parameterDescription, ref ParameterBuildState buildState)
    {
        var attributes = parameterSymbol.GetAttributes();

        var customParserType = attributes.FirstOrDefault(x => x.AttributeClass?.AllInterfaces.Any(y => y.Name == "IArgumentParser") ?? false);
        var hasFromServices = attributes.Any(x => x.AttributeClass?.Name == "FromServicesAttribute");
        var hasFromKeyedServices = attributes.Any(x => x.AttributeClass?.Name == "FromKeyedServicesAttribute");
        var hasArgument = attributes.Any(x => x.AttributeClass?.Name == "ArgumentAttribute");
        var hasValidation = attributes.Any(x => x.AttributeClass?.GetBaseTypes().Any(y => y.Name == "ValidationAttribute") ?? false);
        var isCancellationToken = SymbolEqualityComparer.Default.Equals(parameterSymbol.Type, wellKnownTypes.CancellationToken);
        var isConsoleAppContext = parameterSymbol.Type.Name == "ConsoleAppContext";
        var isHiddenParameter = attributes.Any(x => x.AttributeClass?.Name == "HiddenAttribute");
        var isDefaultValueHidden = attributes.Any(x => x.AttributeClass?.Name == "HideDefaultValueAttribute");

        object? keyedServiceKey = null;
        if (hasFromKeyedServices)
        {
            var attr = attributes.First(x => x.AttributeClass?.Name == "FromKeyedServicesAttribute");
            if (attr.ConstructorArguments.Length != 0)
            {
                keyedServiceKey = attr.ConstructorArguments[0].Value;
            }
        }

        var argumentIndex = -1;
        if (!(hasFromServices || hasFromKeyedServices || isCancellationToken || isConsoleAppContext))
        {
            if (hasArgument)
            {
                argumentIndex = buildState.ArgumentIndexCounter++;
            }
            else
            {
                buildState.ParsableIndex++;
            }
        }

        var isNullableReference = parameterSymbol.NullableAnnotation == NullableAnnotation.Annotated
            && parameterSymbol.Type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T;

        return new CommandParameter
        {
            Name = generatorOptions.DisableNamingConversion ? parameterSymbol.Name : NameConverter.ToKebabCase(parameterSymbol.Name),
            WellKnownTypes = wellKnownTypes,
            OriginalParameterName = parameterSymbol.Name,
            IsNullableReference = isNullableReference,
            IsConsoleAppContext = isConsoleAppContext,
            IsParams = parameterSymbol.IsParams,
            IsHidden = isHiddenParameter,
            IsDefaultValueHidden = isDefaultValueHidden,
            Location = GetParameterLocation(parameterSymbol),
            Type = new EquatableTypeSymbol(parameterSymbol.Type),
            HasDefaultValue = parameterSymbol.HasExplicitDefaultValue,
            DefaultValue = parameterSymbol.HasExplicitDefaultValue ? parameterSymbol.ExplicitDefaultValue : null,
            CustomParserType = customParserType?.AttributeClass?.ToEquatable(),
            IsCancellationToken = isCancellationToken,
            IsFromServices = hasFromServices,
            IsFromKeyedServices = hasFromKeyedServices,
            KeyedServiceKey = keyedServiceKey,
            HasValidation = hasValidation,
            Aliases = parameterDescription.Aliases,
            ArgumentIndex = argumentIndex,
            Description = parameterDescription.Description
        };
    }

    static ParameterDescription TryGetParameterDescription(IReadOnlyDictionary<string, ParameterDescription>? map, string parameterName)
    {
        if (map != null && map.TryGetValue(parameterName, out var result))
        {
            return result;
        }
        return ParameterDescription.Empty;
    }

    static bool HasAttribute(IParameterSymbol parameterSymbol, string attributeName)
    {
        return parameterSymbol.GetAttributes().Any(x => x.AttributeClass?.Name == attributeName);
    }

    Location GetParameterLocation(IParameterSymbol parameterSymbol)
    {
        if (parameterSymbol.DeclaringSyntaxReferences.Length != 0)
        {
            return parameterSymbol.DeclaringSyntaxReferences[0].GetSyntax().GetLocation();
        }
        if (parameterSymbol.Locations.Length != 0)
        {
            return parameterSymbol.Locations[0];
        }
        return node.GetLocation();
    }

    void ParseParameterDescription(string originalDescription, out string[] aliases, out string description)
    {
        // Example:
        // -h|--help, This is a help.

        var splitOne = originalDescription.Split([','], 2);

        // has alias
        if (splitOne[0].TrimStart().StartsWith("-"))
        {
            aliases = splitOne[0].Split(['|'], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            description = splitOne.Length > 1 ? splitOne[1].Trim() : string.Empty;
        }
        else
        {
            aliases = [];
            description = originalDescription;
        }
    }
}
