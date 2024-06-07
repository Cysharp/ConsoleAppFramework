using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConsoleAppFramework;

public interface IFunctionSyntax
{
    DocumentationCommentTriviaSyntax? GetDocumentationCommentTriviaSyntax();
    TypeSyntax ReturnType { get; }
    ParameterListSyntax ParameterList { get; }
    SyntaxList<AttributeListSyntax> AttributeLists { get; }
}

public static class FunctionSyntax
{
    public static IFunctionSyntax? From(SyntaxNode node)
    {
        var syntax = node switch
        {
            MethodDeclarationSyntax x => new FromMethodDeclaration(x),
            LocalFunctionStatementSyntax x => new FromLocalFunctionStatement(x),
            _ => (IFunctionSyntax?)null
        };
        return syntax;
    }

    class FromMethodDeclaration(MethodDeclarationSyntax syntax) : IFunctionSyntax
    {
        public DocumentationCommentTriviaSyntax? GetDocumentationCommentTriviaSyntax() => syntax.GetDocumentationCommentTriviaSyntax();
        public TypeSyntax ReturnType => syntax.ReturnType;
        public ParameterListSyntax ParameterList => syntax.ParameterList;
        public SyntaxList<AttributeListSyntax> AttributeLists => syntax.AttributeLists;
    }

    class FromLocalFunctionStatement(LocalFunctionStatementSyntax syntax) : IFunctionSyntax
    {
        public DocumentationCommentTriviaSyntax? GetDocumentationCommentTriviaSyntax() => syntax.GetDocumentationCommentTriviaSyntax();
        public TypeSyntax ReturnType => syntax.ReturnType;
        public ParameterListSyntax ParameterList => syntax.ParameterList;
        public SyntaxList<AttributeListSyntax> AttributeLists => syntax.AttributeLists;
    }
}
