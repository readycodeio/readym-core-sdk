using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

public static class SerializationHelper
{
    private static readonly Dictionary<SpecialType, string> _specialTypeMap = new()
    {
        { SpecialType.System_Boolean,   "Bool"      },
        { SpecialType.System_Byte,      "Byte"      },
        { SpecialType.System_SByte,     "SByte"     },
        { SpecialType.System_Int16,     "Short"     },
        { SpecialType.System_UInt16,    "UShort"    },
        { SpecialType.System_Int32,     "Int"       },
        { SpecialType.System_UInt32,    "UInt"      },
        { SpecialType.System_Int64,     "Long"      },
        { SpecialType.System_UInt64,    "ULong"     },
        { SpecialType.System_Single,    "Float"     },
        { SpecialType.System_Double,    "Double"    },
        { SpecialType.System_Char,      "Char"      },
        { SpecialType.System_String,    "String"    },
    };

    public static bool IsSerializablePrimitive(SpecialType specialType)
    {
        return _specialTypeMap.ContainsKey(specialType);
    }

    public static string GetDeserializationMethod(SpecialType specialType)
    {
        return _specialTypeMap.TryGetValue(specialType, out var methodName)
            ? $"Get{methodName}"
            : throw new ArgumentException($"Unsupported special type: {specialType}");
    }

    public static SpecialType GetEnumBaseType(ITypeSymbol typeSymbol)
    {
        // if (typeSymbol.BaseType!.SpecialType != SpecialType.System_Enum)
        //     throw new InvalidOperationException($"Type {typeSymbol.Name} is not an enum.");
        //
        // return typeSymbol.BaseType!.BaseType?.SpecialType ?? SpecialType.System_Int32;
        return SpecialType.System_Int32; // TODO
    }

    public static string GetSpecialTypeCSharpName(SpecialType specialType)
    {
        return _specialTypeMap.TryGetValue(specialType, out var name)
            ? name.ToLowerInvariant()
            : throw new ArgumentException($"Unsupported special type: {specialType}");
    }
}