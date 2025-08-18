using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

internal static class GeneratorHelper
{
    private static (string Name, ITypeSymbol Type, int Order, bool ReadOnly) GetMemberEntry(ISymbol symbol)
    {
        if (symbol is IFieldSymbol f)
            return (Name: f.Name, Type: f.Type, Order: f.DeclaringSyntaxReferences[0].Span.Start, ReadOnly: f.IsReadOnly);
        else if (symbol is IPropertySymbol p)
            return (Name: p.Name, Type: p.Type, Order: p.DeclaringSyntaxReferences[0].Span.Start, ReadOnly: p.SetMethod == null);
        else
            throw new InvalidOperationException($"Unsupported symbol type: {symbol.GetType().Name}");
    }

    public static GeneratorTypeInfo GetSymbolInfo(INamedTypeSymbol symbol, bool mapFields, bool mapProperties, bool mapPrivate, bool mapPublic, bool mapInternal)
    {
        var ns = symbol.ContainingNamespace.ToDisplayString();
        var name = symbol.Name;

        var errorMessages = new List<string>();
        var allMembers = new List<(string Name, ITypeSymbol Type, int Order, bool ReadOnly)>();
        foreach (var member in symbol.GetMembers())
        {
            (string Name, ITypeSymbol Type, int Order, bool ReadOnly) entry;

            bool isField;
            var useMember = true;
            var canUseMember = true;

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
                entry = default;
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
                entry = GetMemberEntry(member);
                allMembers.Add(entry);
            }
        }

        var thisNullable = symbol.IsReferenceType && symbol.NullableAnnotation != NullableAnnotation.Annotated;

        return new GeneratorTypeInfo(
            name: name,
            @namespace: ns,
            members: allMembers.ToArray(),
            isNullable: thisNullable,
            errorMessages: errorMessages.ToArray()
        );
    }
}