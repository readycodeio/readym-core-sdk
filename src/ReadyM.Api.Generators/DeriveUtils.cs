using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

internal static class DeriveUtils
{
    internal static INamedTypeSymbol GetTargetSymbol(GeneratorSyntaxContext context, CancellationToken cancellationToken)
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

    internal static DeriveTargetInfo GetTargetInfo(
        bool isExternal,
        INamedTypeSymbol symbol,
        bool emitDirtyMask,
        bool emitBindDelete,
        DeriveMapSettings mapSettings)
    {
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));

        var ns = symbol.ContainingNamespace.ToDisplayString();
        var name = symbol.Name;

        ITypeSymbol? requestedDirtyMaskType = null;
        var allMembers = new List<DeriveMemberInfo>();

        foreach (var member in symbol.GetMembers())
        {
            var errors = new List<string>();
            bool isField;
            var useMember = true;
            var canUseMember = true;
            var canUseMemberFailReasons = new List<string>();

            if (member.Name == "_dirtyMask")
            {
                requestedDirtyMaskType = member switch
                {
                    IFieldSymbol maskField => maskField.Type,
                    IPropertySymbol propField => propField.Type,
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
                canUseMemberFailReasons.Add($"Invalid accessibility: {member.DeclaredAccessibility}");
            }

            if (member.DeclaringSyntaxReferences.Length <= 0)
            {
                useMember = false;
                canUseMember = false;
                canUseMemberFailReasons.Add("No declaring syntax reference");
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
                    canUseMemberFailReasons.Add("Static fields are not supported");
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
                    canUseMemberFailReasons.Add("Static properties are not supported");
                }
                
                if (p is not { GetMethod: not null })
                {
                    canUseMember = false;
                    canUseMemberFailReasons.Add("Properties must have a getter");
                }

                isField = false;
            }
            else
            {
                useMember = false;
                canUseMember = false;
                canUseMemberFailReasons.Add($"Unsupported member type: {member.GetType().Name}");
                isField = false;
            }

            var hasExclude = member.GetAttributes().Any(a => a.AttributeConstructor?.Name == "ExcludeSerializable");
            var hasInclude = member.GetAttributes().Any(a => a.AttributeConstructor?.Name == "IncludeSerializable");

            if (hasInclude && hasExclude)
            {
                errors.Add($"Cannot have `IncludeSerializable` and `ExcludeSerializable` on the same {(isField ? "field" : "property")}: {member.Name}");
            }

            if (hasInclude)
                useMember = true;
            if (hasExclude)
                useMember = false;

            if (!canUseMember && useMember)
            {
                errors.Add($"Cannot use {(isField ? "field" : "property")}: {member.Name}");
                foreach (var reason in canUseMemberFailReasons)
                {
                    errors.Add($" - {reason}");
                }
            }

            if (useMember)
            {
                var fieldInfo = GetMemberInfo(member, errors);
                allMembers.Add(fieldInfo);
            }
        }

        var thisNullable = symbol.IsReferenceType && symbol.NullableAnnotation != NullableAnnotation.Annotated;

        return new DeriveTargetInfo(
            isExternal: isExternal,
            symbol: symbol,
            name: name,
            @namespace: ns,
            members: allMembers.ToArray(),
            isNullable: thisNullable,
            errors: [],
            requestedDirtyMaskType: requestedDirtyMaskType,
            emitDirtyMask: emitDirtyMask,
            emitBindDelete: emitBindDelete,
            mapSettings: mapSettings
        );
    }

    internal static DeriveMemberInfo GetMemberInfo(ISymbol symbol, List<string> errors)
    {
        if (symbol is IFieldSymbol f)
            return new DeriveMemberInfo(
                symbol: symbol,
                name: f.Name,
                type: f.Type,
                order: f.DeclaringSyntaxReferences[0].Span.Start,
                readOnly: f.IsReadOnly,
                errors: errors);
        else if (symbol is IPropertySymbol p)
        {
            if (p.GetMethod == null || p.GetMethod?.IsInitOnly == true || p.SetMethod?.IsInitOnly == true)
                errors.Add($"Properties that are setter-only are not supported: {p.Name}");
            else if (p.GetMethod?.IsInitOnly == true || p.SetMethod?.IsInitOnly == true)
                errors.Add($"Properties that are init-only are not supported: {p.Name}");
            
            return new DeriveMemberInfo(
                symbol: symbol,
                name: p.Name,
                type: p.Type,
                order: p.DeclaringSyntaxReferences[0].Span.Start,
                readOnly: p.SetMethod == null,
                errors: errors);
            
        }
        else
            throw new InvalidOperationException($"Unsupported symbol type: {symbol.GetType().Name}");
    }

    public static string GetGeneratedFileName(INamedTypeSymbol symbol)
        => symbol.Name;
}