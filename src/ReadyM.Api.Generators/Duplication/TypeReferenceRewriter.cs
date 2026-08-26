using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators.Duplication;

/// <summary>
/// Rewrites a copied member so it belongs to the duplicate instead of the original: every reference bound to the
/// source type becomes the target name, constructor names are renamed, and attributes/doc comments are stripped
/// when the request asks for it.
/// </summary>
internal sealed class TypeReferenceRewriter(
    SemanticModel semanticModel,
    INamedTypeSymbol sourceType,
    string targetName,
    bool copyAttributes,
    bool copyDocumentation) : CSharpSyntaxRewriter
{
    private readonly string _sourceName = sourceType.Name;

    public override SyntaxNode? VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
    {
        var rewritten = (ConstructorDeclarationSyntax?)base.VisitConstructorDeclaration(node);

        return rewritten?.WithIdentifier(
            SyntaxFactory.Identifier(targetName).WithTriviaFrom(rewritten.Identifier));
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        if (!IsSourceTypeReference(node))
            return base.VisitIdentifierName(node);

        return SyntaxFactory.IdentifierName(targetName).WithTriviaFrom(node);
    }

    public override SyntaxNode? VisitGenericName(GenericNameSyntax node)
    {
        var rewritten = (GenericNameSyntax?)base.VisitGenericName(node);

        if (rewritten is null || !IsSourceTypeReference(node))
            return rewritten;

        return rewritten.WithIdentifier(
            SyntaxFactory.Identifier(targetName).WithTriviaFrom(rewritten.Identifier));
    }

    public override SyntaxNode? VisitAttributeList(AttributeListSyntax node)
        => copyAttributes ? base.VisitAttributeList(node) : null;

    public override SyntaxTriviaList VisitList(SyntaxTriviaList list)
    {
        var visited = base.VisitList(list);

        if (copyDocumentation)
            return visited;

        return SyntaxFactory.TriviaList(visited.Where(trivia => !IsDocumentationComment(trivia)));
    }

    private bool IsSourceTypeReference(SimpleNameSyntax node)
    {
        // Cheap text gate first: the semantic lookup is the expensive part and almost never needed.
        if (node.Identifier.Text != _sourceName)
            return false;

        // A `Foo` written after a dot is a member access, not a type reference, unless it is qualified with the
        // source type's own namespace, which the symbol check below sorts out.
        var symbol = semanticModel.GetSymbolInfo(node).Symbol
                     ?? semanticModel.GetSymbolInfo(node).CandidateSymbols.FirstOrDefault();

        return symbol is INamedTypeSymbol named &&
               SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, sourceType.OriginalDefinition);
    }

    private static bool IsDocumentationComment(SyntaxTrivia trivia)
        => trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
           trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia) ||
           trivia.IsKind(SyntaxKind.DocumentationCommentExteriorTrivia);

    /// <summary>Rewrites a list of members in one pass, dropping any the rewriter removed entirely.</summary>
    public IEnumerable<MemberDeclarationSyntax> RewriteAll(IEnumerable<MemberDeclarationSyntax> members)
    {
        foreach (var member in members)
        {
            if (Visit(member) is MemberDeclarationSyntax rewritten)
                yield return rewritten;
        }
    }
}
