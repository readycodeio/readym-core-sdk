using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Derive.Cpp;
using ReadyM.Api.Generators.TypeTranslation.Model;

namespace ReadyM.Api.Generators;

public static class DeriveCppUtils
{
    public static string CppTypeNamespace(ITypeSymbol type)
    {
        var fullName = CppTypeTranslationPipeline.TypeTranslation.Translate(type);
        var lastSep = fullName.LastIndexOf("::", StringComparison.Ordinal);
        return lastSep == -1 ? "" : fullName.Substring(0, lastSep);
    }

    public static string CppTypeName(ITypeSymbol type)
        => CppTypeTranslationPipeline.TypeTranslation.Translate(type);

    public static string CppPath(ITypeSymbol type)
        => CppTypeTranslationPipeline.PathTranslation.Translate(type);

    public static IReadOnlyList<string> CppPaths(ITypeSymbol type)
    {
        void Recur(ITypeName typeName, List<string> result)
        {
            var translated = CppTypeTranslationPipeline.PathTranslation.Translate(typeName);
            var rendered = CppTypeTranslationPipeline.PathTranslation.Render(translated);
            if (!string.IsNullOrEmpty(rendered))
                result.Add(rendered);
            
            if (typeName is GenericInstanceName genericInst)
            {
                Recur(genericInst.GenericDefinition, result);
                foreach (var arg in genericInst.TypeArguments)
                {
                    Recur(arg, result);
                }
            }
        }
        
        var typeName = CppTypeTranslationPipeline.PathTranslation.Parse(type);
        var paths = new List<string>();
        Recur(typeName, paths);
        return paths;
    }

    public static string GetCppDefaultValue(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum)
            return "{}";

        return type.SpecialType switch
        {
            SpecialType.System_Boolean => "false",
            SpecialType.System_Byte => "0",
            SpecialType.System_SByte => "0",
            SpecialType.System_Int16 => "0",
            SpecialType.System_UInt16 => "0",
            SpecialType.System_Int32 => "0",
            SpecialType.System_UInt32 => "0",
            SpecialType.System_Int64 => "0",
            SpecialType.System_UInt64 => "0",
            SpecialType.System_Single => "0.0f",
            SpecialType.System_Double => "0.0",
            SpecialType.System_Char => "0",
            _ => type.IsReferenceType ? "nullptr" : "{}"
        };
    }
}