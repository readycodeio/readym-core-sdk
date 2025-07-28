using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

internal static class GeneratorHelper
{
    private static (string Name, ITypeSymbol Type, int Order) GetMember(ISymbol symbol)
    {
        if (symbol is IFieldSymbol f)
            return (Name: f.Name, Type: f.Type, Order: f.DeclaringSyntaxReferences[0].Span.Start);
        else if (symbol is IPropertySymbol p)
            return (Name: p.Name, Type: p.Type, Order: p.DeclaringSyntaxReferences[0].Span.Start);
        else
            throw new InvalidOperationException($"Unsupported symbol type: {symbol.GetType().Name}");
    }

    public static GeneratorTypeInfo GetSymbolInfo(INamedTypeSymbol symbol, bool requireFields, bool requirePublic)
    {
        var ns = symbol.ContainingNamespace.ToDisplayString();
        var name = symbol.Name;

        var writeFields = symbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f is { IsStatic: false, IsReadOnly: false })
            .Where(f => !requirePublic || f is { DeclaredAccessibility : Accessibility.Public })
            .Where(f => f.DeclaringSyntaxReferences.Length > 0)
            .Select(GetMember)
            .ToArray();
        var allFields = symbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => f is { IsStatic: false })
            .Where(f => !requirePublic || f is { DeclaredAccessibility : Accessibility.Public })
            .Where(f => f.DeclaringSyntaxReferences.Length > 0)
            .Select(GetMember)
            .ToArray();

        (string Name, ITypeSymbol Type, int Order)[] writeProps;
        (string Name, ITypeSymbol Type, int Order)[] allProps;

        if (requireFields)
        {
            writeProps = [];
            allProps = [];
        }
        else
        {
            writeProps = symbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p is { IsStatic: false, GetMethod: not null, SetMethod: not null, DeclaredAccessibility : Accessibility.Public })
                .Where(f => f.DeclaringSyntaxReferences.Length > 0) 
                .Select(GetMember)
                .ToArray();
            allProps = symbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p is { IsStatic: false, GetMethod: not null, DeclaredAccessibility : Accessibility.Public })
                .Where(f => f.DeclaringSyntaxReferences.Length > 0) 
                .Select(GetMember)
                .ToArray();
        }

        var useCons = writeFields.Length != allFields.Length || writeProps.Length != allProps.Length;

        (string Name, ITypeSymbol Type, int Order)[] fields;
        if (useCons)
        {
            fields = allFields.Concat(allProps).ToArray();
        }
        else
        {
            fields = writeFields.Concat(writeProps).ToArray();
        }

        var thisNullable = symbol.IsReferenceType && symbol.NullableAnnotation != NullableAnnotation.Annotated;
        
        return new GeneratorTypeInfo
        {
            Namespace = ns,
            Name = name,
            Fields = fields,
            IsNullable = thisNullable,
            UseCons = useCons,
        };
    }
}