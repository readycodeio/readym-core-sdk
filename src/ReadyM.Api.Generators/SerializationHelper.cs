using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

public static class SerializationHelper
{
    private static readonly Dictionary<SpecialType, string> SpecialTypeMap = new()
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

    public static string GetDeserializationMethod(SpecialType specialType)
        => SpecialTypeMap.TryGetValue(specialType, out var methodName)
            ? $"Get{methodName}"
            : throw new ArgumentException($"Unsupported special type: {specialType}");

    public static SpecialType GetEnumBaseType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol { TypeKind: TypeKind.Enum } enumSymbol)
            throw new ArgumentException("Type symbol must be an enum.", nameof(typeSymbol));

        var underlyingType = enumSymbol.EnumUnderlyingType;
        if (underlyingType is null)
            throw new InvalidOperationException($"Enum '{typeSymbol.ToDisplayString()}' does not have an underlying type.");

        return underlyingType.SpecialType;
    }

    public static string GetSpecialTypeCSharpName(SpecialType specialType)
        => SpecialTypeMap.TryGetValue(specialType, out var name)
            ? name.ToLowerInvariant()
            : throw new ArgumentException($"Unsupported special type: {specialType}");

    public static bool IsSerializablePrimitive(SpecialType specialType)
        => SpecialTypeMap.ContainsKey(specialType);

    public static bool IsINetSerializable(ITypeSymbol type)
    {
        const string fullName = "LiteNetLib.Utils.INetSerializable";
        return type.AllInterfaces.Any(i => i.ContainingNamespace.ToDisplayString() + "." + i.Name == fullName);
    }

    public static bool IsEquatable(ITypeSymbol type)
        => type.AllInterfaces.Any(i =>
            i.ContainingNamespace.ToDisplayString() == "System" && i is { Name: "IEquatable", TypeArguments.Length: 1 } && SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], type));

    public static bool IsDeltaEquatable(ITypeSymbol type)
        => type.AllInterfaces.Any(i =>
            i.ContainingNamespace.ToDisplayString() == "ReadyM.Api.Serialization" && i is { Name: "IDeltaEquatable", TypeArguments.Length: 1 } && SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], type));
    
    internal static bool IsVectorLike(ITypeSymbol type)
        => (type.Name is "Vector2" or "Vector3" or "Vector4") &&
           type.ContainingNamespace.ToDisplayString() == "System.Numerics";
    
    internal static bool HasSerializeMethod(ITypeSymbol type)
        => type.GetMembers("Serialize")
            .OfType<IMethodSymbol>()
            .Any(m =>
                m.Parameters.Length == 1 &&
                m.Parameters[0].Type.ToDisplayString() == "LiteNetLib.Utils.NetDataWriter");

    internal static bool HasDeserializeMethod(ITypeSymbol type)
        => type.GetMembers("Deserialize")
            .OfType<IMethodSymbol>()
            .Any(m =>
                m.Parameters.Length == 1 &&
                m.Parameters[0].Type.ToDisplayString() == "LiteNetLib.Utils.NetDataReader");
}