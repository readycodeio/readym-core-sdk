using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

internal static class DeriveUtils
{
    internal const string FloatComparisonEpsilon = "0.1f";
    internal const string DoubleComparisonEpsilon = "0.1";
    internal const string VectorComparisonEpsilon = "0.01f";

    internal static INamedTypeSymbol GetAttributedSymbol(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var node = context.Node;
        var model = context.SemanticModel.Compilation.GetSemanticModel(node.SyntaxTree);
        var symbol = model.GetDeclaredSymbol(node, cancellationToken) as INamedTypeSymbol;
        if (symbol == null)
            throw new InvalidOperationException("Expected an INamedTypeSymbol for the attributed struct.");

        return symbol;
    }
    
    internal static DeriveMapSettings GetMapSettings(byte mode)
        => new(
            mapFields: (mode & (1 << 0)) != 0,
            mapProperties: (mode & (1 << 1)) != 0,
            mapPrivate: (mode & (1 << 2)) != 0,
            mapPublic: (mode & (1 << 3)) != 0,
            mapInternal: (mode & (1 << 4)) != 0);

    internal static DeriveTargetInfo GetTargetInfo(INamedTypeSymbol symbol, bool emitDirtyMask, DeriveMapSettings mapSettings)
    {
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));

        var ns = symbol.ContainingNamespace.ToDisplayString();
        var name = symbol.Name;

        string? dirtyMaskType = null;
        var errorMessages = new List<string>();
        var allMembers = new List<DeriveMemberInfo>();

        foreach (var member in symbol.GetMembers())
        {
            bool isField;
            var useMember = true;
            var canUseMember = true;

            if (member.Name == "_dirtyMask")
            {
                dirtyMaskType = member switch
                {
                    IFieldSymbol maskField => maskField.Type.ToDisplayString(),
                    IPropertySymbol propField => propField.Type.ToDisplayString(),
                    _ => throw new InvalidOperationException($"Unsupported symbol type for dirty mask: {member.GetType().Name}")
                };
                continue;
            }

            if (member.DeclaredAccessibility == Accessibility.Private)
            {
                if (!mapSettings.MapPrivate)
                    useMember = false;
            }
            else if (member.DeclaredAccessibility == Accessibility.Public)
            {
                if (!mapSettings.MapPublic)
                    useMember = false;
            }
            else if (member.DeclaredAccessibility == Accessibility.Internal)
            {
                if (!mapSettings.MapInternal)
                    useMember = false;
            }
            else
            {
                useMember = false;
                canUseMember = false;
            }

            if (member.DeclaringSyntaxReferences.Length <= 0)
            {
                useMember = false;
                canUseMember = false;
            }

            if (member is IFieldSymbol f)
            {
                if (!mapSettings.MapFields)
                    useMember = false;

                if (f is { IsStatic: true })
                {
                    // Static readonly fields are not serialized
                    useMember = false;
                    canUseMember = false;
                }

                if (f is { IsReadOnly: true })
                {
                    canUseMember = false;
                }

                isField = true;
            }
            else if (member is IPropertySymbol p)
            {
                if (!mapSettings.MapProperties)
                    useMember = false;

                if (p is { IsStatic: true })
                {
                    useMember = false;
                    canUseMember = false;
                }

                if (p is not { GetMethod: not null, SetMethod: not null })
                {
                    canUseMember = false;
                }

                isField = false;
            }
            else
            {
                useMember = false;
                canUseMember = false;
                isField = false;
            }

            var hasExclude = member.GetAttributes().Any(a => a.AttributeConstructor?.Name == "ExcludeSerializable");
            var hasInclude = member.GetAttributes().Any(a => a.AttributeConstructor?.Name == "IncludeSerializable");

            if (hasInclude && hasExclude)
            {
                errorMessages.Add($"Cannot have `IncludeSerializable` and `ExcludeSerializable` on the same {(isField ? "field" : "property")}: {member.Name}");
                continue;
            }

            if (hasInclude)
                useMember = true;
            if (hasExclude)
                useMember = false;

            if (!canUseMember && useMember)
            {
                errorMessages.Add($"Cannot use {(isField ? "field" : "property")}: {member.Name}");
                continue;
            }

            if (useMember)
            {
                var fieldInfo = GetMemberInfo(member);
                allMembers.Add(fieldInfo);
            }
        }

        var thisNullable = symbol.IsReferenceType && symbol.NullableAnnotation != NullableAnnotation.Annotated;

        return new DeriveTargetInfo(
            name: name,
            @namespace: ns,
            members: allMembers.ToArray(),
            isNullable: thisNullable,
            errorMessages: errorMessages.ToArray(),
            dirtyMaskType: dirtyMaskType,
            emitDirtyMask: emitDirtyMask,
            mapSettings: mapSettings
        );
    }

    internal static DeriveMemberInfo GetMemberInfo(ISymbol symbol)
    {
        if (symbol is IFieldSymbol f)
            return new DeriveMemberInfo(
                name: f.Name,
                type: f.Type,
                order: f.DeclaringSyntaxReferences[0].Span.Start,
                readOnly: f.IsReadOnly,
                isInvalid: false);
        else if (symbol is IPropertySymbol p)
            return new DeriveMemberInfo(
                name: p.Name,
                type: p.Type,
                order: p.DeclaringSyntaxReferences[0].Span.Start,
                readOnly: p.SetMethod == null,
                isInvalid: p.GetMethod == null || p.GetMethod?.IsInitOnly == true || p.SetMethod?.IsInitOnly == true);
        else
            throw new InvalidOperationException($"Unsupported symbol type: {symbol.GetType().Name}");
    }

    public static string GetGeneratedFileName(INamedTypeSymbol symbol)
        => symbol.ContainingNamespace != null ? $"{symbol.ContainingNamespace.ToDisplayString()}.{symbol.Name}" : symbol.Name;
}