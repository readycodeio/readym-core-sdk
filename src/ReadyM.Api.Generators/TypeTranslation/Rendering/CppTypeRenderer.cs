using System;
using System.Collections.Generic;
using System.Text;
using ReadyM.Api.Generators.TypeTranslation.Model;

namespace ReadyM.Api.Generators.TypeTranslation.Rendering;

public sealed class CppTypeRenderer : ITypeRenderer
{
    public string Render(ITypeName typeName)
    {
        if (TryRenderSpecialType(typeName, out var renderedSpecialType))
            return renderedSpecialType;
        
        return RenderNonSpecial(typeName);
    }
    
    private string RenderNonSpecial(ITypeName typeName)
    {
        return typeName switch
        {
            TypeName typeNameLeaf => typeNameLeaf.Name,
            TypeParam typeParam => typeParam.Name,
            Numeric numeric => numeric.Value.ToString(),
            QualifiedName qualifiedName => $"{RenderNonSpecial(qualifiedName.Prefix)}::{RenderNonSpecial(qualifiedName.InnerType)}",
            GenericInstanceName genericInstanceName => RenderGeneric(genericInstanceName),
            EmptyName => "",
            _ => throw new NotSupportedException($"Unsupported type name kind: {typeName.GetType().FullName}"),
        };
    }

    private string RenderGeneric(GenericInstanceName genericInstanceName)
    {
        var builder = new StringBuilder();
        builder.Append(Render(genericInstanceName.GenericDefinition));
        builder.Append('<');

        for (var i = 0; i < genericInstanceName.TypeArguments.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(Render(genericInstanceName.TypeArguments[i]));
        }

        builder.Append('>');
        return builder.ToString();
    }

    private static bool TryRenderSpecialType(ITypeName typeName, out string rendered)
    {
        var parts = GetQualifiedParts(typeName);
        if (parts is null)
        {
            rendered = string.Empty;
            return false;
        }

        rendered = parts.Count switch
        {
            1 => RenderShortSpecialTypeName(parts[0]),
            2 when parts[0] is "System" => RenderLongSpecialTypeName(parts[1]),
            _ => string.Empty,
        };

        return rendered.Length > 0;
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

    private static string RenderShortSpecialTypeName(string name) => name switch
    {
        "bool" => "bool",
        "byte" => "uint8_t",
        "sbyte" => "int8_t",
        "short" => "int16_t",
        "ushort" => "uint16_t",
        "int" => "int32_t",
        "uint" => "uint32_t",
        "long" => "int64_t",
        "ulong" => "uint64_t",
        "float" => "float",
        "double" => "double",
        "string" => "Interop::String",
        "char" => "wchar_t",
        _ => string.Empty,
    };

    private static string RenderLongSpecialTypeName(string name) => name switch
    {
        "Boolean" => "bool",
        "Byte" => "uint8_t",
        "SByte" => "int8_t",
        "Int16" => "int16_t",
        "UInt16" => "uint16_t",
        "Int32" or "Integer" => "int32_t",
        "UInt32" => "uint32_t",
        "Int64" => "int64_t",
        "UInt64" => "uint64_t",
        "Single" => "float",
        "Double" => "double",
        "String" => "Interop::String",
        "Char" => "wchar_t",
        _ => string.Empty,
    };
}