using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace ConsoleAppFramework;

internal static class RoslynExtensions
{
    internal static string ToFullyQualifiedFormatDisplayString(this ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    public static bool EqualsUnconstructedGenericType(this INamedTypeSymbol left, INamedTypeSymbol right)
    {
        var l = left.IsGenericType ? left.ConstructUnboundGenericType() : left;
        var r = right.IsGenericType ? right.ConstructUnboundGenericType() : right;
        return SymbolEqualityComparer.Default.Equals(l, r);
    }

    public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this INamedTypeSymbol type, bool includeSelf = false)
    {
        if (includeSelf) yield return type;
        var baseType = type.BaseType;
        while (baseType != null)
        {
            yield return baseType;
            baseType = baseType.BaseType;
        }
    }

    public static bool EqualsNamespaceAndName(this ITypeSymbol? left, ITypeSymbol? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;

        var l = left.ContainingNamespace;
        var r = right.ContainingNamespace;
        while (l != null && r != null)
        {
            if (l.Name != r.Name) return false;

            l = l.ContainingNamespace;
            r = r.ContainingNamespace;
        }

        return (left.Name == right.Name);
    }

    public static bool ZipEquals<T>(this IEnumerable<T> left, IEnumerable<T> right, Func<T, T, bool> predicate)
        where T : IMethodSymbol
    {
        using var e1 = left.GetEnumerator();
        using var e2 = right.GetEnumerator();
        while (true)
        {
            var b1 = e1.MoveNext();
            var b2 = e2.MoveNext();

            if (b1 != b2) return false;   // different sequence length, ng
            if (b1 == false) return true; // both false, ok

            if (!predicate(e1.Current, e2.Current))
            {
                return false;
            }
        }
    }

    public static ParameterListSyntax? GetParameterListOfConstructor(this SyntaxNode node)
    {
        if (node is ConstructorDeclarationSyntax ctor)
        {
            return ctor.ParameterList;
        }
        else if (node is ClassDeclarationSyntax primaryCtor)
        {
            return primaryCtor.ParameterList;
        }
        else
        {
            return null;
        }
    }

    public static Location Clone(this Location location)
    {
        // without inner SyntaxTree
        return Location.Create(location.SourceTree?.FilePath ?? "", location.SourceSpan, location.GetLineSpan().Span);
    }

    public static DocumentationCommentTriviaSyntax? GetDocumentationCommentTriviaSyntax(this SyntaxNode node)
    {
        // Hack note:
        // ISymbol.GetDocumentationCommentXml requires<GenerateDocumentationFile>true</>.
        // However, getting the DocumentationCommentTrivia of a SyntaxNode also requires the same condition.
        // It can only be obtained when DocumentationMode is Parse or Diagnostic, but when<GenerateDocumentationFile>false</>,
        // it becomes None, and the necessary Trivia cannot be obtained.
        // Therefore, we will attempt to reparse and retrieve it.

        // About DocumentationMode and Trivia: https://github.com/dotnet/roslyn/issues/58210
        if (node.SyntaxTree.Options.DocumentationMode == DocumentationMode.None)
        {
            var withDocumentationComment = node.SyntaxTree.Options.WithDocumentationMode(DocumentationMode.Parse);
            var code = node.ToFullString();
            var newTree = CSharpSyntaxTree.ParseText(code, (CSharpParseOptions)withDocumentationComment);
            node = newTree.GetRoot();
        }

        foreach (var leadingTrivia in node.GetLeadingTrivia())
        {
            if (leadingTrivia.GetStructure() is DocumentationCommentTriviaSyntax structure)
            {
                return structure;
            }
        }

        return null;
    }

    static IEnumerable<XmlNodeSyntax> GetXmlElements(this SyntaxList<XmlNodeSyntax> content, string elementName)
    {
        foreach (XmlNodeSyntax syntax in content)
        {
            if (syntax is XmlEmptyElementSyntax emptyElement)
            {
                if (string.Equals(elementName, emptyElement.Name.ToString(), StringComparison.Ordinal))
                {
                    yield return emptyElement;
                }

                continue;
            }

            if (syntax is XmlElementSyntax elementSyntax)
            {
                if (string.Equals(elementName, elementSyntax.StartTag?.Name?.ToString(), StringComparison.Ordinal))
                {
                    yield return elementSyntax;
                }

                continue;
            }
        }
    }

    public static string GetSummary(this DocumentationCommentTriviaSyntax docComment)
    {
        var summary = docComment.Content.GetXmlElements("summary").FirstOrDefault() as XmlElementSyntax;
        if (summary == null) return "";

        return NormalizeDocCommentText(summary.Content.ToString());
    }

    public static IEnumerable<(string Name, string Description)> GetParams(this DocumentationCommentTriviaSyntax docComment)
    {
        foreach (var item in docComment.Content.GetXmlElements("param").OfType<XmlElementSyntax>())
        {
            var name = item.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault()?.Identifier.Identifier.ValueText.Replace("///", "").Trim() ?? "";
            var desc = item.Content.ToString().Replace("///", "").Trim() ?? "";
            yield return (name, desc);
        }

        yield break;
    }

    static string NormalizeDocCommentText(string text)
    {
        var lines = text.Replace("///", "").Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

        var start = 0;
        var end = lines.Length - 1;
        while (start <= end && string.IsNullOrWhiteSpace(lines[start]))
        {
            start++;
        }
        while (end >= start && string.IsNullOrWhiteSpace(lines[end]))
        {
            end--;
        }

        if (start > end) return "";

        var normalized = new string[end - start + 1];
        for (var i = start; i <= end; i++)
        {
            normalized[i - start] = lines[i].Trim();
        }
        return string.Join("\n", normalized);
    }

    public static void GetConstantValues(this ArgumentListSyntax argumentListSyntax, SemanticModel model,
        string name1, string name2,
        ref object? value1, ref object? value2)
    {
        var arguments = argumentListSyntax.Arguments;
        for (int i = 0; i < arguments.Count; i++)
        {
            var arg = arguments[i];
            var constant = model.GetConstantValue(arg.Expression);
            if (constant.HasValue)
            {
                var constantValue = constant.Value;
                if (arg.NameColon != null)
                {
                    var name = arg.NameColon.Name.Identifier.Text;
                    if (name == name1)
                    {
                        value1 = constantValue;
                    }
                    else if (name == name2)
                    {
                        value2 = constantValue;
                    }
                }
                else
                {
                    if (i == 0) value1 = constantValue;
                    else if (i == 1) value2 = constantValue;
                }
            }
        }
    }

    public static void GetConstantValues(this ArgumentListSyntax argumentListSyntax, SemanticModel model,
        string name1, string name2, string name3,
        ref object? value1, ref object? value2, ref object? value3)
    {
        var arguments = argumentListSyntax.Arguments;
        for (int i = 0; i < arguments.Count; i++)
        {
            var arg = arguments[i];
            var constant = model.GetConstantValue(arg.Expression);
            if (constant.HasValue)
            {
                var constantValue = constant.Value;
                if (arg.NameColon != null)
                {
                    var name = arg.NameColon.Name.Identifier.Text;
                    if (name == name1)
                    {
                        value1 = constantValue;
                    }
                    else if (name == name2)
                    {
                        value2 = constantValue;
                    }
                    else if (name == name3)
                    {
                        value3 = constantValue;
                    }
                }
                else
                {
                    if (i == 0) value1 = constantValue;
                    else if (i == 1) value2 = constantValue;
                    else if (i == 2) value3 = constantValue;
                }
            }
        }
    }

}
