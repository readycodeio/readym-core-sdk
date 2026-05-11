using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.TypeTranslation.Model;

namespace ReadyM.Api.Generators.TypeTranslation.Parsing;

public sealed class RoslynTypeNameParser : ITypeNameParser
{
    public ITypeName Parse(ITypeSymbol typeSymbol) => typeSymbol switch
    {
        ITypeParameterSymbol typeParameterSymbol => new TypeParam(typeParameterSymbol.Name),
        INamedTypeSymbol namedTypeSymbol => ParseNamedType(namedTypeSymbol),
        IArrayTypeSymbol arrayTypeSymbol => throw new NotSupportedException($"Arrays are not supported: {arrayTypeSymbol}"),
        IPointerTypeSymbol pointerTypeSymbol => throw new NotSupportedException($"Pointers are not supported: {pointerTypeSymbol}"),
        _ => throw new NotSupportedException($"Unsupported type symbol kind: {typeSymbol.Kind}"),
    };

    private static ITypeName ParseNamedType(INamedTypeSymbol typeSymbol)
    {
        if (TryGetSpecialTypeAlias(typeSymbol, out var alias))
            return new TypeName(alias);

        var baseName = ParseQualifiedMetadataName(typeSymbol);
        return typeSymbol.IsGenericType
            ? new GenericInstanceName(baseName, [.. typeSymbol.TypeArguments.Select(ParseTypeArgument)])
            : baseName;
    }

    private static ITypeName ParseTypeArgument(ITypeSymbol typeSymbol) => typeSymbol switch
    {
        ITypeParameterSymbol typeParameterSymbol => new TypeParam(typeParameterSymbol.Name),
        INamedTypeSymbol namedTypeSymbol => ParseNamedType(namedTypeSymbol),
        _ => throw new NotSupportedException($"Unsupported type argument kind: {typeSymbol.Kind}"),
    };

    private static ITypeName ParseQualifiedMetadataName(INamedTypeSymbol typeSymbol)
    {
        var parts = new List<string>();
        CollectContainingParts(typeSymbol, parts);

        ITypeName current = new TypeName(parts[0]);
        for (var i = 1; i < parts.Count; i++)
        {
            current = new QualifiedName(current, new TypeName(parts[i]));
        }

        return current;
    }

    private static void CollectContainingParts(INamedTypeSymbol typeSymbol, List<string> parts)
    {
        if (typeSymbol.ContainingNamespace is { IsGlobalNamespace: false })
        {
            CollectNamespaceParts(typeSymbol.ContainingNamespace, parts);
        }

        if (typeSymbol.ContainingType is not null)
        {
            CollectContainingTypeParts(typeSymbol.ContainingType, parts);
        }

        parts.Add(RemoveGenericArity(typeSymbol.Name));
    }

    private static void CollectContainingTypeParts(INamedTypeSymbol typeSymbol, List<string> parts)
    {
        if (typeSymbol.ContainingType is not null)
        {
            CollectContainingTypeParts(typeSymbol.ContainingType, parts);
        }

        parts.Add(RemoveGenericArity(typeSymbol.Name));
    }

    private static void CollectNamespaceParts(INamespaceSymbol namespaceSymbol, List<string> parts)
    {
        if (namespaceSymbol.ContainingNamespace is { IsGlobalNamespace: false })
        {
            CollectNamespaceParts(namespaceSymbol.ContainingNamespace, parts);
        }

        parts.Add(namespaceSymbol.Name);
    }

    private static string RemoveGenericArity(string name)
    {
        var tickIndex = name.IndexOf('`');
        return tickIndex >= 0 ? name.Substring(0, tickIndex) : name;
    }

    private static bool TryGetSpecialTypeAlias(INamedTypeSymbol typeSymbol, out string alias)
    {
        alias = typeSymbol.SpecialType switch
        {
            SpecialType.System_Boolean => "bool",
            SpecialType.System_Byte => "byte",
            SpecialType.System_Char => "char",
            SpecialType.System_Decimal => "decimal",
            SpecialType.System_Double => "double",
            SpecialType.System_Int16 => "short",
            SpecialType.System_Int32 => "int",
            SpecialType.System_Int64 => "long",
            SpecialType.System_Object => "object",
            SpecialType.System_SByte => "sbyte",
            SpecialType.System_Single => "float",
            SpecialType.System_String => "string",
            SpecialType.System_UInt16 => "ushort",
            SpecialType.System_UInt32 => "uint",
            SpecialType.System_UInt64 => "ulong",
            _ => string.Empty,
        };

        return alias.Length > 0;
    }
}