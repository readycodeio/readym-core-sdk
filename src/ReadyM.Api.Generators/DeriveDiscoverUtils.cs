using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadyM.Api.Generators;

internal static class DeriveDiscoverUtils
{
    internal sealed class TargetCandidate(
        bool isExternal,
        INamedTypeSymbol symbol,
        AttributeData? attribute,
        Location? location,
        GeneratorSyntaxContext? context)
    {
        public bool IsExternal { get; } = isExternal;
        public INamedTypeSymbol Symbol { get; } = symbol;
        public AttributeData? Attribute { get; } = attribute;
        public Location? Location { get; } = location;
        public GeneratorSyntaxContext? Context { get; } = context;
    }
    
    internal sealed class AssemblyFieldAttributeCandidate(
        INamedTypeSymbol forType,
        string? forField,
        AttributeData attribute)
    {
        public INamedTypeSymbol ForType { get; } = forType;
        public string? ForField { get; } = forField;
        public AttributeData Attribute { get; } = attribute;
    }

    public static bool TypePredicate(
        SyntaxNode syntaxNode,
        CancellationToken cancellationToken,
        params string[] attributeNames)
    {
        if (cancellationToken.IsCancellationRequested)
            return false;

        var structDecl = syntaxNode as StructDeclarationSyntax;
        if (structDecl == null || structDecl.AttributeLists.Count == 0)
            return false;

        var attributes = structDecl.AttributeLists.SelectMany(static x => x.Attributes).ToList();
        return attributes.Any(x => IsAttributeName(x.Name, attributeNames));
    }

    public static bool AssemblyPredicate(
        SyntaxNode syntaxNode,
        CancellationToken cancellationToken,
        params string[] attributeNames)
    {
        if (cancellationToken.IsCancellationRequested)
            return false;

        var attribute = syntaxNode as AttributeSyntax;
        if (attribute == null)
            return false;

        var attributeList = attribute.Parent as AttributeListSyntax;
        if (attributeList == null)
            return false;

        if (attributeList.Target == null || attributeList.Target.Identifier.Text != "assembly")
            return false;

        return IsAttributeName(attribute.Name, attributeNames);
    }
    
    public static bool AssemblyFieldAttributePredicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return false;

        var attribute = syntaxNode as AttributeSyntax;
        if (attribute == null)
            return false;

        var attributeList = attribute.Parent as AttributeListSyntax;
        if (attributeList == null)
            return false;

        if (attributeList.Target == null || attributeList.Target.Identifier.Text != "assembly")
            return false;

        var argumentList = attribute.ArgumentList;
        if (argumentList == null)
            return false;

        var hasForType = argumentList.Arguments.Any(static x =>
            x.NameEquals is { Name.Identifier.Text: "forType" } ||
            x.NameColon is { Name.Identifier.Text: "forType" });

        if (!hasForType)
            return false;

        var hasForField = argumentList.Arguments.Any(static x =>
            x.NameEquals is { Name.Identifier.Text: "forField" } ||
            x.NameColon is { Name.Identifier.Text: "forField" });

        return hasForField;
    }

    public static bool IsAttributeName(NameSyntax name, params string[] attributeNames)
    {
        if (name is IdentifierNameSyntax identifierName)
            return IsAttributeIdentifier(identifierName.Identifier.Text, attributeNames);

        if (name is QualifiedNameSyntax qualifiedName)
            return IsAttributeName(qualifiedName.Right, attributeNames);

        if (name is AliasQualifiedNameSyntax aliasQualifiedName)
            return IsAttributeIdentifier(aliasQualifiedName.Name.Identifier.Text, attributeNames);

        return false;
    }

    public static TargetCandidate? TransformTypeLevel(
        GeneratorSyntaxContext context,
        CancellationToken ct,
        string attributeMetadataName)
    {
        if (ct.IsCancellationRequested)
            return null;

        var symbol = DeriveUtils.GetTargetSymbol(context, ct);

        var attr = AttributeUtils.GetAttributeData(
            symbol,
            attributeMetadataName);

        return new TargetCandidate(
            false,
            symbol,
            attr,
            symbol.Locations.FirstOrDefault(),
            context);
    }

    public static TargetCandidate? TransformAssemblyLevel(
        GeneratorSyntaxContext context,
        CancellationToken ct,
        string attributeMetadataName,
        string? skipAttributePropertyName = null)
    {
        if (ct.IsCancellationRequested)
            return null;

        var attributeSyntax = context.Node as AttributeSyntax;
        if (attributeSyntax == null)
            return null;

        var attr = AttributeUtils.GetAttributeData(
            context.SemanticModel.Compilation.Assembly,
            attributeMetadataName,
            attributeSyntax);

        if (attr == null)
            return null;

        if (!string.IsNullOrEmpty(skipAttributePropertyName))
        {
            var skip = AttributeUtils.GetAttributeValue<bool>(
                attr,
                skipAttributePropertyName!,
                false);

            if (skip)
                return null;
        }
        
        var targetSymbol = AttributeUtils.GetAttributeValue<INamedTypeSymbol?>(
            attr,
            "forType",
            null);

        if (targetSymbol == null)
            return null;

        return new TargetCandidate(
            true,
            targetSymbol,
            attr,
            attributeSyntax.GetLocation(),
            null);
    }
    
    public static AssemblyFieldAttributeCandidate? TransformAssemblyFieldAttribute(GeneratorSyntaxContext context, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return null;

        var attributeSyntax = context.Node as AttributeSyntax;
        if (attributeSyntax == null)
            return null;

        var attr = AttributeUtils.GetAttributeData(
            context.SemanticModel.Compilation.Assembly,
            attributeSyntax);

        if (attr == null)
            return null;

        var forType = AttributeUtils.GetAttributeValue<INamedTypeSymbol?>(
            attr,
            "forType",
            null);

        if (forType == null)
            return null;

        var forField = AttributeUtils.GetAttributeValue<string?>(
            attr,
            "forField",
            null);

        if (string.IsNullOrEmpty(forField))
            return null;

        return new AssemblyFieldAttributeCandidate(
            forType,
            forField,
            attr);
    }

    public static IReadOnlyList<AttributeData> GetAssemblyFieldAttributesFor(
        INamedTypeSymbol symbol,
        IEnumerable<AssemblyFieldAttributeCandidate?> fieldAttributes)
    {
        return fieldAttributes
            .Where(x =>
                x != null &&
                SymbolEqualityComparer.Default.Equals(x.ForType, symbol))
            .Select(x => x!.Attribute)
            .ToArray();
    }

    private static bool IsAttributeIdentifier(string identifier, IEnumerable<string> attributeNames)
    {
        foreach (var attributeName in attributeNames)
        {
            if (identifier == attributeName)
                return true;

            if (attributeName.EndsWith("Attribute") &&
                identifier == attributeName.Substring(0, attributeName.Length - "Attribute".Length))
                return true;

            if (!attributeName.EndsWith("Attribute") &&
                identifier == attributeName + "Attribute")
                return true;
        }

        return false;
    }
}
