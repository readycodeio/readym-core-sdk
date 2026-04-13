using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.Cpp;

internal abstract class CppFieldTypeSupportBase : ICppFieldTypeSupport
{
    public abstract bool CanHandle(ITypeSymbol type);

    private static readonly Dictionary<string, string> NamespaceReplacements = new()
    {
        ["ReadyM.Relay.Common.Oblivion"] = "RM",
        ["ReadyM.Relay.Common"] = "RM",
        ["ReadyM.Relay"] = "RM",
        ["ReadyM.Api.Multiplayer"] = "RM",
        ["ReadyM.Api"] = "RM",
        ["ReadyM"] = "RM",
        ["System.Numerics"] = "Interop",
        ["System"] = "Interop",
    };
    
    private static string GetCppFullTypeName(string fullName)
    {
        fullName = fullName.Replace("global::", "");
        
        foreach (var d in NamespaceReplacements)
        {
            var dottedPrefix = d.Key + ".";
            if (fullName.StartsWith(dottedPrefix))
            {
                fullName = d.Value + "::" + fullName.Substring(dottedPrefix.Length);
                break;
            }
        }

        var parts = fullName.Split('.') ?? [];
        if (parts.Length > 0 && parts[0] == "ReadyM")
        {
            parts[0] = "RM";
        }
        return string.Join("::", parts);
    }

    public virtual string GetCppTypeName(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_Boolean => "bool",
            SpecialType.System_Byte => "uint8_t",
            SpecialType.System_SByte => "int8_t",
            SpecialType.System_Int16 => "int16_t",
            SpecialType.System_UInt16 => "uint16_t",
            SpecialType.System_Int32 => "int32_t",
            SpecialType.System_UInt32 => "uint32_t",
            SpecialType.System_Int64 => "int64_t",
            SpecialType.System_UInt64 => "uint64_t",
            SpecialType.System_Single => "float",
            SpecialType.System_Double => "double",
            SpecialType.System_Char => "char16_t",
            _ => GetCppFullTypeName(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
        };
    }

    public virtual string GetCppDefaultValue(ITypeSymbol type)
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

    public abstract string BuildSetterCondition(DeriveMemberModel model);
}