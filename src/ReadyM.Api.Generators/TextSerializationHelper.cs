using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

public static class TextSerializationHelper
{
    private static readonly Dictionary<SpecialType, (string WriteName, string ReadName)> _specialTypeMap = new()
    {
        { SpecialType.System_Boolean,   ("Boolean", "Boolean"   )},
        { SpecialType.System_Byte,      ("Number",  "Byte"      )},
        { SpecialType.System_SByte,     ("Number",  "SByte"     )},
        { SpecialType.System_Int16,     ("Number",  "Int16"     )},
        { SpecialType.System_UInt16,    ("Number",  "UInt16"    )},
        { SpecialType.System_Int32,     ("Number",  "Int32"     )},
        { SpecialType.System_UInt32,    ("Number",  "UInt32"    )},
        { SpecialType.System_Int64,     ("Number",  "Int64"     )},
        { SpecialType.System_UInt64,    ("Number",  "UInt64"    )},
        { SpecialType.System_Single,    ("Number",  "Single"    )},
        { SpecialType.System_Double,    ("Number",  "Double"    )},
        { SpecialType.System_Char,      ("Number",  "UInt16"    )},
        { SpecialType.System_String,    ("String",  "String"    )},
    };
    
    public static bool IsSerializablePrimitive(SpecialType specialType)
    {
        return _specialTypeMap.ContainsKey(specialType);
    }

    public static string GetReadMethod(SpecialType specialType)
    {
        return _specialTypeMap.TryGetValue(specialType, out var d)
            ? $"Get{d.ReadName}"
            : throw new ArgumentException($"Unsupported special type: {specialType}");
    }

    public static string GetWriteMethod(SpecialType specialType)
    {
        return _specialTypeMap.TryGetValue(specialType, out var d)
            ? $"Write{d.WriteName}"
            : throw new ArgumentException($"Unsupported special type: {specialType}");
    }
}