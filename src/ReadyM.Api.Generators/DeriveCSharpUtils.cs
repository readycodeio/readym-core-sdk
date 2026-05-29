using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators;

internal static class DeriveCSharpUtils
{
    public static string FullyQualifiedTypeName(ITypeSymbol type)
    {
        var typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var questionMark = type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
        return $"{typeName}{questionMark}".Replace("global::", "");
    }

    public static string GetTypeModifiers(TypeDeclarationSyntax declaration, bool forceUnsafe)
    {
        var modifiers = declaration.Modifiers
            .Where(static x =>
                x.IsKind(SyntaxKind.PublicKeyword) ||
                x.IsKind(SyntaxKind.InternalKeyword) ||
                x.IsKind(SyntaxKind.ProtectedKeyword) ||
                x.IsKind(SyntaxKind.PrivateKeyword) ||
                x.IsKind(SyntaxKind.UnsafeKeyword) ||
                x.IsKind(SyntaxKind.PartialKeyword))
            .Select(static x => x.Text)
            .ToList();

        if (forceUnsafe && !modifiers.Contains("unsafe"))
        {
            var partialIndex = modifiers.IndexOf("partial");

            if (partialIndex >= 0)
                modifiers.Insert(partialIndex, "unsafe");
            else
                modifiers.Add("unsafe");
        }

        if (!modifiers.Contains("partial"))
            modifiers.Add("partial");

        return string.Join(" ", modifiers);
    }
}