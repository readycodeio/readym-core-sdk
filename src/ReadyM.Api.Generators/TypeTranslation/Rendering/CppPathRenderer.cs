using System;
using System.Collections.Generic;
using ReadyM.Api.Generators.TypeTranslation.Model;

namespace ReadyM.Api.Generators.TypeTranslation.Rendering;

internal sealed class CppPathRenderer : ITypeRenderer
{
    public string Render(ITypeName typeName)
    {
        var name = RenderName(typeName);
        if (string.IsNullOrEmpty(name))
            return "";
        
        return name + ".h";
    }
    
    public string RenderName(ITypeName typeName)
    {
        if (IsSpecialType(typeName))
            return "";

        return RenderNonSpecial(typeName);
    }

    private string RenderNonSpecial(ITypeName typeName)
    {
        var path = typeName switch
        {
            TypeName typeNameLeaf => typeNameLeaf.Name,
            TypeParam typeParam => typeParam.Name,
            Numeric numeric => numeric.Value.ToString(),
            QualifiedName qualifiedName => Combine(RenderNonSpecial(qualifiedName.Prefix), RenderNonSpecial(qualifiedName.InnerType)),
            GenericInstanceName genericInstanceName => RenderNonSpecial(genericInstanceName.GenericDefinition),
            EmptyName => "",
            _ => throw new NotSupportedException($"Unsupported type name kind: {typeName.GetType().FullName}"),
        };

        if (string.IsNullOrEmpty(path))
            return "";
        
        return path;
    }

    private static bool IsSpecialType(ITypeName typeName)
    {
        var parts = GetQualifiedParts(typeName);
        if (parts is null)
            return false;

        return parts.Count switch
        {
            1 => IsShortSpecialTypeName(parts[0]),
            2 when parts[0] is "System" => IsLongSpecialTypeName(parts[1]),
            _ => false,
        };
    }

    private static IReadOnlyList<string>? GetQualifiedParts(ITypeName typeName) => typeName switch
    {
        TypeName typeNameLeaf => [typeNameLeaf.Name],
        QualifiedName qualifiedName => GetQualifiedParts(qualifiedName, []),
        EmptyName => [],
        _ => null,
    };

    private static IReadOnlyList<string>? GetQualifiedParts(ITypeName typeName, List<string> parts) => typeName switch
    {
        TypeName typeNameLeaf => AddPart(parts, typeNameLeaf.Name),
        QualifiedName qualifiedName => GetQualifiedParts(qualifiedName.InnerType, [.. GetQualifiedParts(qualifiedName.Prefix, parts)!]),
        EmptyName => [],
        _ => null,
    };

    private static IReadOnlyList<string> AddPart(List<string> parts, string name)
    {
        parts.Add(name);
        return parts;
    }

    private static bool IsShortSpecialTypeName(string name) => name switch
    {
        "bool" => true,
        "byte" => true,
        "sbyte" => true,
        "short" => true,
        "ushort" => true,
        "int" => true,
        "uint" => true,
        "long" => true,
        "ulong" => true,
        "float" => true,
        "double" => true,
        "string" => true,
        "char" => true,
        _ => false,
    };

    private static bool IsLongSpecialTypeName(string name) => name switch
    {
        "Boolean" => true,
        "Byte" => true,
        "SByte" => true,
        "Int16" => true,
        "UInt16" => true,
        "Int32" or "Integer" => true,
        "UInt32" => true,
        "Int64" => true,
        "UInt64" => true,
        "Single" => true,
        "Double" => true,
        "String" => true,
        "Char" => true,
        _ => false,
    };
    
    private static string Combine(string prefix, string inner)
    {
        if (string.IsNullOrEmpty(prefix))
            return inner;
        if (string.IsNullOrEmpty(inner))
            return prefix;
        return $"{prefix}/{inner}";
    }
}