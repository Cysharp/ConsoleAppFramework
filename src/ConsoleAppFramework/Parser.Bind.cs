using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConsoleAppFramework;

/// <summary>
/// Constants for attribute names used in binding discovery.
/// </summary>
internal static class AttributeNames
{
    public const string Argument = "ArgumentAttribute";
}

/// <summary>
/// Constants for special markers in XML documentation.
/// </summary>
internal static class BindingMarkers
{
    public const string Argument = "argument";
}

/// <summary>
/// Represents a parameter that may have [Bind] attribute for object binding.
/// </summary>
/// <param name="Parameter">The command parameter.</param>
/// <param name="HasBind">Whether the parameter has [Bind] attribute.</param>
/// <param name="BindPrefix">Explicit prefix from [Bind] attribute, if any.</param>
/// <param name="TypeSymbol">The type symbol of the parameter.</param>
internal record BindParameterCandidate(
    CommandParameter Parameter,
    bool HasBind,
    string? BindPrefix,
    ITypeSymbol TypeSymbol);

/// <summary>
/// Context for object binding discovery, passed during type analysis.
/// </summary>
internal record ObjectBindingDiscoveryContext(
    string Prefix,
    string[] ParentPath,
    Location DiagnosticLocation,
    HashSet<ITypeSymbol> VisitedTypes,
    ITypeSymbol? GlobalOptionsType = null)
{
    /// <summary>Creates initial context for starting discovery.</summary>
    public static ObjectBindingDiscoveryContext Create(
        string prefix, Location diagnosticLocation, ITypeSymbol? globalOptionsType = null) =>
        new(prefix, [], diagnosticLocation, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default), globalOptionsType);
}

internal partial class Parser
{
    CommandParameter[]? ProcessBindParameters(BindParameterCandidate[] candidates, Location diagnosticLocation)
    {
        // Count how many parameters have [Bind] to determine prefix behavior
        var bindCount = candidates.Count(c => c.HasBind);

        var result = new List<CommandParameter>();
        foreach (var candidate in candidates)
        {
            if (!candidate.HasBind)
            {
                result.Add(candidate.Parameter);
                continue;
            }

            // Determine prefix:
            // - If an explicit prefix is provided, use it
            // - If multiple [Bind] parameters, use parameter name as prefix
            // - If single [Bind] parameter, use no prefix (empty string)
            var prefix = candidate.BindPrefix
                ?? (bindCount > 1 ? candidate.Parameter.Name : "");

            // Create ObjectBindingInfo for this parameter
            // Pass knownGlobalOptionsType so inheritance from global options can be detected
            var objectBinding = DiscoverObjectBinding(
                candidate.TypeSymbol,
                prefix,
                diagnosticLocation,
                knownGlobalOptionsType);

            if (objectBinding == null)
            {
                return null; // Error already reported
            }

            // Create new parameter with ObjectBinding
            result.Add(candidate.Parameter with { ObjectBinding = objectBinding });
        }

        return result.ToArray();
    }

    ObjectBindingInfo? DiscoverObjectBinding(
        ITypeSymbol type,
        string prefix,
        Location diagnosticLocation,
        ITypeSymbol? globalOptionsType = null)
    {
        var ctx = ObjectBindingDiscoveryContext.Create(prefix, diagnosticLocation, globalOptionsType);
        return DiscoverObjectBindingCore(type, ctx);
    }

    ObjectBindingInfo? DiscoverObjectBindingCore(ITypeSymbol type, ObjectBindingDiscoveryContext ctx)
    {
        // Check for circular references
        if (!ctx.VisitedTypes.Add(type))
        {
            context.ReportDiagnostic(DiagnosticDescriptors.BindCircularReference, ctx.DiagnosticLocation, type.ToDisplayString());
            return null;
        }

        // Check if this type inherits from a [GlobalOptions] type
        var (globalOptionsBaseType, globalOptionsType) = FindGlobalOptionsBaseType(type, ctx.GlobalOptionsType);

        // Get constructors
        var publicConstructors = type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(x => x.MethodKind == Microsoft.CodeAnalysis.MethodKind.Constructor && x.DeclaredAccessibility == Accessibility.Public)
            .ToArray();

        if (publicConstructors.Length > 1)
        {
            // Check if one is parameterless
            var parameterlessCtors = publicConstructors.Where(x => x.Parameters.Length == 0).ToArray();
            if (parameterlessCtors.Length == 0)
            {
                context.ReportDiagnostic(DiagnosticDescriptors.BindMultipleConstructors, ctx.DiagnosticLocation, type.ToDisplayString());
                return null;
            }
            // Use parameterless constructor
            publicConstructors = parameterlessCtors;
        }

        if (publicConstructors.Length == 0)
        {
            context.ReportDiagnostic(DiagnosticDescriptors.BindTypeNoValidConstructor, ctx.DiagnosticLocation, type.ToDisplayString());
            return null;
        }

        var constructor = publicConstructors[0];
        var hasPrimaryConstructor = constructor.Parameters.Length > 0;
        var ctorParameters = new List<ConstructorParameterInfo>();
        var properties = new List<BindablePropertyInfo>();

        // Extract XML documentation from the type (for primary constructor param docs)
        Dictionary<string, string>? paramDescriptions = null;
        if (type.DeclaringSyntaxReferences.Length > 0)
        {
            var typeSyntax = type.DeclaringSyntaxReferences[0].GetSyntax();
            var typeDocComment = typeSyntax.GetDocumentationCommentTriviaSyntax();
            if (typeDocComment != null)
            {
                paramDescriptions = typeDocComment.GetParams().ToDictionary(x => x.Name, x => x.Description);
            }
        }

        // Track argument index counter for [Argument] within the object
        int argumentIndexCounter = 0;

        // Process constructor parameters
        for (int i = 0; i < constructor.Parameters.Length; i++)
        {
            var ctorParam = constructor.Parameters[i];

            // Get description from XML documentation (param tag in type's doc comment)
            var rawDescription = "";
            if (paramDescriptions != null && paramDescriptions.TryGetValue(ctorParam.Name, out var desc))
            {
                rawDescription = desc;
            }

            // Parse aliases and argument marker from description
            ParseBindableDescription(rawDescription, out var ctorAliases, out var ctorParamDescription, out var isArgumentByComment);

            // Check for [Argument] attribute on constructor parameter
            var hasArgumentAttr = ctorParam.GetAttributes().Any(a => a.AttributeClass?.Name == AttributeNames.Argument);

            int argumentIndex = (hasArgumentAttr || isArgumentByComment) ? argumentIndexCounter++ : -1;

            // Boolean properties default to false because they act as CLI flags:
            // presence of the flag means true (e.g., --verbose), absence means false.
            // This allows boolean options to work without requiring an explicit value.
            var ctorIsBoolType = ctorParam.Type.SpecialType == SpecialType.System_Boolean;
            var ctorHasDefault = ctorParam.HasExplicitDefaultValue || ctorIsBoolType;
            var ctorDefaultValue = ctorParam.HasExplicitDefaultValue ? ctorParam.ExplicitDefaultValue : ctorIsBoolType ? false : null;

            ctorParameters.Add(new ConstructorParameterInfo
            {
                Name = ctorParam.Name,
                Type = new EquatableTypeSymbol(ctorParam.Type),
                HasDefaultValue = ctorHasDefault,
                DefaultValue = ctorDefaultValue,
                Index = i,
                ArgumentIndex = argumentIndex
            });

            // Create a CLI property for this constructor parameter
            var isBoolType = ctorParam.Type.SpecialType == SpecialType.System_Boolean;
            var hasDefault = ctorParam.HasExplicitDefaultValue || isBoolType;
            var defaultValue = ctorParam.HasExplicitDefaultValue ? ctorParam.ExplicitDefaultValue : isBoolType ? false : null;
            var isRequired = !hasDefault;

            string cliName;
            if (hasArgumentAttr || isArgumentByComment)
            {
                cliName = $"[{argumentIndex}]"; // Positional argument marker
            }
            else
            {
                cliName = BuildCliName(ctx.Prefix, ctorParam.Name);
            }

            // Check if this parameter comes from the global options type
            var isFromGlobalOptions = globalOptionsType != null &&
                IsMemberDeclaredInType(ctorParam, globalOptionsType);

            // Compute ParseInfo for this property's type
            var ctorParseInfo = TypeParseHelper.AnalyzeType(ctorParam.Type, wellKnownTypes);

            properties.Add(new BindablePropertyInfo
            {
                CliName = cliName,
                Type = new EquatableTypeSymbol(ctorParam.Type),
                HasDefaultValue = hasDefault,
                DefaultValue = defaultValue,
                PropertyName = ctorParam.Name,
                PropertyAccessPath = ctorParam.Name,
                ParentPath = ctx.ParentPath,
                Description = ctorParamDescription,
                Aliases = ctorAliases,
                IsRequired = isRequired,
                IsConstructorParameter = true,
                ConstructorParameterIndex = i,
                IsInitOnly = false,
                ArgumentIndex = argumentIndex,
                IsFromGlobalOptions = isFromGlobalOptions,
                ParseInfo = ctorParseInfo
            });
        }

        // Process public settable properties (that are not already handled by constructor)
        // Include inherited properties from base types
        var ctorParamNames = new HashSet<string>(constructor.Parameters.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);
        var settableProperties = GetAllPublicProperties(type)
            .Where(p => !p.IsStatic &&
                        !p.IsReadOnly &&
                        (p.SetMethod != null || p.IsRequired) &&
                        !ctorParamNames.Contains(p.Name))
            .ToArray();

        foreach (var prop in settableProperties)
        {
            // Check if this is a parsable type or a nested object
            if (IsParsableType(prop.Type))
            {
                // Get description from property's XML documentation (summary tag)
                var rawPropDescription = GetPropertyDescription(prop);

                // Parse aliases and argument marker from description
                ParseBindableDescription(rawPropDescription, out var propAliases, out var propDescription, out var isArgumentByComment);

                // Check for [Argument] attribute on property
                var hasArgumentAttr = prop.GetAttributes().Any(a => a.AttributeClass?.Name == AttributeNames.Argument);

                int argumentIndex = (hasArgumentAttr || isArgumentByComment) ? argumentIndexCounter++ : -1;

                var hasDefaultValue = !prop.IsRequired && !IsNonNullableReferenceType(prop);
                var isRequired = prop.IsRequired || IsNonNullableReferenceType(prop);
                var isInitOnly = prop.SetMethod?.IsInitOnly ?? false;

                string cliName;
                if (hasArgumentAttr || isArgumentByComment)
                {
                    cliName = $"[{argumentIndex}]"; // Positional argument marker
                }
                else
                {
                    cliName = BuildCliName(ctx.Prefix, prop.Name);
                }

                // Check if this property comes from the global options type
                var isFromGlobalOptions = globalOptionsType != null &&
                    IsMemberDeclaredInType(prop, globalOptionsType);

                // Try to get the property initializer value
                var propDefaultValue = hasDefaultValue ? GetPropertyInitializerValue(prop) : null;

                // Compute ParseInfo for this property's type
                var propParseInfo = TypeParseHelper.AnalyzeType(prop.Type, wellKnownTypes);

                properties.Add(new BindablePropertyInfo
                {
                    CliName = cliName,
                    Type = new EquatableTypeSymbol(prop.Type),
                    HasDefaultValue = hasDefaultValue,
                    DefaultValue = propDefaultValue,
                    PropertyName = prop.Name,
                    PropertyAccessPath = prop.Name,
                    ParentPath = ctx.ParentPath,
                    Description = propDescription,
                    Aliases = propAliases,
                    IsRequired = isRequired,
                    IsConstructorParameter = false,
                    ConstructorParameterIndex = -1,
                    IsInitOnly = isInitOnly,
                    ArgumentIndex = argumentIndex,
                    IsFromGlobalOptions = isFromGlobalOptions,
                    ParseInfo = propParseInfo
                });
            }
        }

        // Remove the type from visited set when returning (for sibling types)
        ctx.VisitedTypes.Remove(type);

        return new ObjectBindingInfo
        {
            BoundType = new EquatableTypeSymbol(type),
            Properties = properties.ToArray(),
            HasPrimaryConstructor = hasPrimaryConstructor,
            ConstructorParameters = ctorParameters.ToArray(),
            CustomPrefix = ctx.Prefix,
            GlobalOptionsBaseType = globalOptionsBaseType
        };
    }

    /// <summary>
    /// Gets all public properties from a type including inherited properties from base types.
    /// </summary>
    IEnumerable<IPropertySymbol> GetAllPublicProperties(ITypeSymbol type)
    {
        var seenNames = new HashSet<string>();
        var currentType = type;

        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in currentType.GetMembers())
            {
                if (member is IPropertySymbol { DeclaredAccessibility: Accessibility.Public } prop
                    && seenNames.Add(prop.Name)) // Only add if not already seen (handles overrides)
                {
                    yield return prop;
                }
            }
            currentType = currentType.BaseType;
        }
    }

    /// <summary>
    /// Checks if a member (property or parameter) is declared in a specific type.
    /// </summary>
    bool IsMemberDeclaredInType(ISymbol member, ITypeSymbol targetType)
    {
        // For parameters, check the containing method's containing type
        if (member is IParameterSymbol param)
        {
            // Parameters in a constructor that come from a base type's primary constructor
            // are not directly declared in the base type, so we check the property with the same name
            var containingType = param.ContainingSymbol?.ContainingType;
            if (containingType != null)
            {
                // Check if the base type has a property with this name
                var baseType = targetType;
                while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
                {
                    var matchingProp = baseType.GetMembers(param.Name)
                        .OfType<IPropertySymbol>()
                        .FirstOrDefault();
                    if (matchingProp != null && SymbolEqualityComparer.Default.Equals(matchingProp.ContainingType, targetType))
                    {
                        return true;
                    }
                    baseType = baseType.BaseType;
                }
            }
            return false;
        }

        // For properties, check if they're declared in the target type
        if (member is IPropertySymbol prop)
        {
            return SymbolEqualityComparer.Default.Equals(prop.ContainingType, targetType);
        }

        return false;
    }

    string BuildCliName(string prefix, string propertyName)
    {
        var kebabProperty = generatorOptions.DisableNamingConversion ? propertyName : NameConverter.ToKebabCase(propertyName);
        if (string.IsNullOrEmpty(prefix))
        {
            return $"--{kebabProperty}";
        }
        var kebabPrefix = generatorOptions.DisableNamingConversion ? prefix : NameConverter.ToKebabCase(prefix);
        return $"--{kebabPrefix}-{kebabProperty}";
    }

    bool IsParsableType(ITypeSymbol type)
    {
        // Nullable<T>
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            var underlyingType = ((INamedTypeSymbol)type).TypeArguments[0];
            return IsParsableType(underlyingType);
        }

        // Primitives
        switch (type.SpecialType)
        {
            case SpecialType.System_String:
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
            case SpecialType.System_Decimal:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_DateTime:
                return true;
        }

        // Enum
        if (type.TypeKind == TypeKind.Enum)
        {
            return true;
        }

        // Array of ISpanParsable elements
        if (type.TypeKind == TypeKind.Array)
        {
            var elementType = ((IArrayTypeSymbol)type).ElementType;
            var parsable = wellKnownTypes.ISpanParsable;
            if (parsable != null && elementType.AllInterfaces.Any(x => x.EqualsUnconstructedGenericType(parsable)))
            {
                return true;
            }
            // Arrays of non-ISpanParsable elements are handled via JSON fallback
            return true;
        }

        // Known types with TryParse
        if (wellKnownTypes.HasTryParse(type))
        {
            return true;
        }

        // ISpanParsable<T>
        var spanParsable = wellKnownTypes.ISpanParsable;
        if (spanParsable != null && type.AllInterfaces.Any(x => x.EqualsUnconstructedGenericType(spanParsable)))
        {
            return true;
        }

        return false;
    }

    bool IsNonNullableReferenceType(IPropertySymbol property) =>
        property is { NullableAnnotation: NullableAnnotation.NotAnnotated, Type.IsReferenceType: true }
        && property.Type.SpecialType != SpecialType.System_String; // String is special-cased as nullable

    /// <summary>
    /// Extracts the XML documentation summary from a property.
    /// </summary>
    string GetPropertyDescription(IPropertySymbol property)
    {
        if (property.DeclaringSyntaxReferences.Length == 0)
            return "";

        var syntax = property.DeclaringSyntaxReferences[0].GetSyntax();
        var docComment = syntax.GetDocumentationCommentTriviaSyntax();
        if (docComment == null)
            return "";

        return docComment.GetSummary();
    }

    /// <summary>
    /// Extracts the initializer value from a property if it's a constant literal.
    /// </summary>
    object? GetPropertyInitializerValue(IPropertySymbol property)
    {
        if (property.DeclaringSyntaxReferences.Length == 0)
            return null;

        var syntax = property.DeclaringSyntaxReferences[0].GetSyntax();

        // Handle PropertyDeclarationSyntax (e.g., public int Port { get; set; } = 8080;)
        if (syntax is PropertyDeclarationSyntax propDecl)
        {
            var initializer = propDecl.Initializer?.Value;
            if (initializer != null)
            {
                return ExtractLiteralValue(initializer);
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts a constant value from a literal expression.
    /// </summary>
    object? ExtractLiteralValue(SyntaxNode literalNode)
    {
        if (literalNode is LiteralExpressionSyntax literal)
            return literal.Token.Value;

        // Handle prefix unary expressions like -5
        if (literalNode is not PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax operandLiteral } prefix)
            return null;

        var value = operandLiteral.Token.Value;
        if (prefix.OperatorToken.Text == "-" && value != null)
        {
            return value switch
            {
                int i => -i,
                long l => -l,
                float f => -f,
                double d => -d,
                decimal m => -m,
                _ => null
            };
        }

        return null;
    }

    /// <summary>
    /// Parses a description to extract aliases, argument marker, and the actual description text.
    /// Format: "-h|--host, Description text" for aliases
    /// Format: "argument, Description text" to mark as positional argument (case insensitive)
    /// </summary>
    void ParseBindableDescription(string originalDescription, out string[] aliases, out string description, out bool isArgument)
    {
        // Examples:
        // -h|--help, This is a help.
        // argument, This is a positional argument.

        var splitOne = originalDescription.Split([','], 2);
        var prefix = splitOne[0].Trim();

        // Check for "argument" prefix (case insensitive)
        if (prefix.Equals(BindingMarkers.Argument, StringComparison.OrdinalIgnoreCase))
        {
            aliases = [];
            isArgument = true;
            description = splitOne.Length > 1 ? splitOne[1].Trim() : string.Empty;
        }
        // Check for alias prefix (starts with -)
        else if (prefix.StartsWith("-"))
        {
            aliases = prefix.Split(['|'], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            isArgument = false;
            description = splitOne.Length > 1 ? splitOne[1].Trim() : string.Empty;
        }
        else
        {
            aliases = [];
            isArgument = false;
            description = originalDescription;
        }
    }

    /// <summary>
    /// Finds the global options base type for a given type, if any.
    /// Only matches against a known GlobalOptions type (from ConfigureGlobalOptions&lt;T&gt;()).
    /// </summary>
    /// <param name="type">The type to check for GlobalOptions inheritance.</param>
    /// <param name="knownGlobalOptionsType">The GlobalOptions type registered via ConfigureGlobalOptions&lt;T&gt;().</param>
    /// <returns>A tuple of (EquatableTypeSymbol for the base type, the actual ITypeSymbol) or (null, null).</returns>
    static (EquatableTypeSymbol? baseType, ITypeSymbol? globalOptionsType) FindGlobalOptionsBaseType(
        ITypeSymbol type, ITypeSymbol? knownGlobalOptionsType)
    {
        // Only check if we have a known GlobalOptions type from ConfigureGlobalOptions<T>()
        if (knownGlobalOptionsType == null)
        {
            return (null, null);
        }

        // Check if the type inherits from the known GlobalOptions type
        for (var baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(baseType, knownGlobalOptionsType))
            {
                return (new EquatableTypeSymbol(knownGlobalOptionsType), knownGlobalOptionsType);
            }
        }

        return (null, null);
    }
}
