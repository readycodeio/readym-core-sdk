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
        {
            return renderedSpecialType;
        }

        return typeName switch
        {
            TypeName typeNameLeaf => typeNameLeaf.Name,
            TypeParam typeParam => typeParam.Name,
            Numeric numeric => numeric.Value.ToString(),
            QualifiedName qualifiedName => $"{Render(qualifiedName.Prefix)}::{Render(qualifiedName.InnerType)}",
            GenericInstanceName genericInstanceName => RenderGeneric(genericInstanceName),
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
            1 => RenderSpecialTypeName(parts[0]),
            2 when parts[0] is "System" => RenderSpecialTypeName(parts[1]),
            _ => string.Empty,
        };

        return rendered.Length > 0;
    }

    private static IReadOnlyList<string>? GetQualifiedParts(ITypeName typeName) => typeName switch
    {
        TypeName typeNameLeaf => [typeNameLeaf.Name],
        QualifiedName qualifiedName => GetQualifiedParts(qualifiedName, []),
        _ => null,
    };

    private static IReadOnlyList<string>? GetQualifiedParts(ITypeName typeName, List<string> parts) => typeName switch
    {
        TypeName typeNameLeaf => AddPart(parts, typeNameLeaf.Name),
        QualifiedName qualifiedName => GetQualifiedParts(qualifiedName.InnerType, [.. GetQualifiedParts(qualifiedName.Prefix, parts)!]),
        _ => null,
    };

    private static IReadOnlyList<string> AddPart(List<string> parts, string name)
    {
        parts.Add(name);
        return parts;
    }

    private static string RenderSpecialTypeName(string name) => name switch
    {
        "bool" or "Boolean" => "bool",
        "byte" or "Byte" => "uint8_t",
        "sbyte" or "SByte" => "int8_t",
        "short" or "Int16" => "int16_t",
        "ushort" or "UInt16" => "uint16_t",
        "int" or "Int32" or "Integer" => "int32_t",
        "uint" or "UInt32" => "uint32_t",
        "long" or "Int64" => "int64_t",
        "ulong" or "UInt64" => "uint64_t",
        "float" or "Single" => "float",
        "double" or "Double" => "double",
        "string" or "String" => "Interop::String",
        "char" or "Char" => "wchar_t",
        _ => string.Empty,
    };
}