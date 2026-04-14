using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

internal static class GeneratorHelper
{
    private static GeneratorField GetMemberEntry(ISymbol symbol)
    {
        if (symbol is IFieldSymbol f)
            return new GeneratorField(
                name: f.Name,
                type: f.Type,
                order: f.DeclaringSyntaxReferences[0].Span.Start,
                readOnly: f.IsReadOnly,
                isInvalid: false);
        else if (symbol is IPropertySymbol p)
            return new GeneratorField(
                name: p.Name,
                type: p.Type,
                order: p.DeclaringSyntaxReferences[0].Span.Start,
                readOnly: p.SetMethod == null,
                isInvalid: p.GetMethod == null || p.GetMethod?.IsInitOnly == true || p.SetMethod?.IsInitOnly == true);
        else
            throw new InvalidOperationException($"Unsupported symbol type: {symbol.GetType().Name}");
    }

    public static GeneratorTypeInfo GetSymbolInfo(
        INamedTypeSymbol symbol,
        bool mapFields,
        bool mapProperties,
        bool mapPrivate,
        bool mapPublic,
        bool mapInternal)
    {
        var ns = symbol.ContainingNamespace.ToDisplayString();
        var name = symbol.Name;

        string? dirtyMaskType = null;
        var errorMessages = new List<string>();
        var allMembers = new List<GeneratorField>();
        
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
                    _ => null
                };
                continue;
            }
            
            if (member.DeclaredAccessibility == Accessibility.Private)
            {
                if (!mapPrivate)
                    useMember = false;
            }
            else if (member.DeclaredAccessibility == Accessibility.Public)
            {
                if (!mapPublic)
                    useMember = false;
            }
            else if (member.DeclaredAccessibility == Accessibility.Internal)
            {
                if (!mapInternal)
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
                if (!mapFields)
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
                if (!mapProperties)
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
                var fieldInfo = GetMemberEntry(member);
                allMembers.Add(fieldInfo);
            }
        }

        var thisNullable = symbol.IsReferenceType && symbol.NullableAnnotation != NullableAnnotation.Annotated;

        return new GeneratorTypeInfo(
            name: name,
            @namespace: ns,
            members: allMembers.ToArray(),
            isNullable: thisNullable,
            errorMessages: errorMessages.ToArray(),
            dirtyMaskType: dirtyMaskType
        );
    }
}