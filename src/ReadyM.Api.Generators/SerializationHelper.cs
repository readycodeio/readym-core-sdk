using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    private static readonly List<SpecialType> PrimitiveTypeList = // https://learn.microsoft.com/en-us/dotnet/api/system.type.isprimitive?view=net-10.0
    [
        SpecialType.System_Boolean,
        SpecialType.System_Byte,
        SpecialType.System_SByte,
        SpecialType.System_Int16,
        SpecialType.System_UInt16,
        SpecialType.System_Int32,
        SpecialType.System_UInt32,
        SpecialType.System_Int64,
        SpecialType.System_UInt64,
        SpecialType.System_IntPtr,
        SpecialType.System_UIntPtr,
        SpecialType.System_Char,
        SpecialType.System_Single,
        SpecialType.System_Double,
    ];

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

    internal static bool IsPrimitive(ITypeSymbol type)
        => PrimitiveTypeList.Contains(type.SpecialType);

    internal static bool IsNativeContainer(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
            return false;
        else if (IsNativeList(namedType, out _))
            return true;
        else if (IsNativeFixed(namedType, out _, out _))
            return true;
        else if (IsNativeDictionary(namedType, out _, out _, out _))
            return true;
        else if (IsNativeRingBuffer(namedType, out _, out _))
            return true;
        else if (IsNativeString(namedType, out _))
            return true;
        else
            return false;
    }
    
    internal static bool IsNativeList(
        ITypeSymbol type,
        [NotNullWhen(true)] out ITypeSymbol? itemType)
    {
        if (type is INamedTypeSymbol { IsGenericType: true, Name: "NativeList" } namedType &&
            namedType.ContainingNamespace.ToDisplayString() == "Yooni.Native.Container" &&
            namedType.TypeArguments.Length == 1)
        {
            itemType = namedType.TypeArguments[0];
            return true;
        }
        
        itemType = null;
        return false;
    }
    
    internal static bool IsNativeStorage(
        ITypeSymbol type,
        [NotNullWhen(true)] out ITypeSymbol? itemType, 
        [NotNullWhen(true)] out int? size)
    {
        for (var candidateSize = 1; candidateSize <= 256; candidateSize *= 2)
        {
            if (type is INamedTypeSymbol { IsGenericType: true } namedType &&
                namedType.Name == $"Storage{candidateSize}" &&
                namedType.ContainingNamespace.ToDisplayString() == "Yooni.Native.LowLevel" &&
                namedType.TypeArguments.Length == 1)
            {
                itemType = namedType.TypeArguments[0];
                size = candidateSize;
                return true;
            }
        }
        
        itemType = null;
        size = null;
        return false;
    }
    
    internal static bool IsNativeFixed(
        ITypeSymbol type,
        [NotNullWhen(true)] out ITypeSymbol? itemType, 
        [NotNullWhen(true)] out int? size)
    {
        if (type is INamedTypeSymbol { IsGenericType: true, Name: "NativeFixed" } namedType &&
            namedType.ContainingNamespace.ToDisplayString() == "Yooni.Native.Container" &&
            namedType.TypeArguments.Length == 2)
        {
            itemType = namedType.TypeArguments[0];
            var storageType = namedType.TypeArguments[1];
            if (IsNativeStorage(storageType, out var itemType0, out size) && 
                SymbolEqualityComparer.Default.Equals(itemType0, itemType))
                return true;
        }
        
        itemType = null;
        size = null;
        return false;
    }
    
    internal static bool IsNativeDictionary(
        ITypeSymbol type,
        [NotNullWhen(true)] out ITypeSymbol? keyType, 
        [NotNullWhen(true)] out ITypeSymbol? valueType,
        [NotNullWhen(true)] out ITypeSymbol? hashType)
    {
        if (type is INamedTypeSymbol { IsGenericType: true, Name: "NativeDictionary" } namedType &&
            namedType.ContainingNamespace.ToDisplayString() == "Yooni.Native.Container" &&
            namedType.TypeArguments.Length == 3)
        {
            keyType = namedType.TypeArguments[0];
            valueType = namedType.TypeArguments[1];
            hashType = namedType.TypeArguments[2];
            return true;
        }
        
        keyType = null;
        valueType = null;
        hashType = null;
        return false;
    }
    
    internal static bool IsNativeHashCollection(
        ITypeSymbol type,
        [NotNullWhen(true)] out ITypeSymbol? keyType, 
        [NotNullWhen(true)] out ITypeSymbol? valueType)
    {
        if (type is INamedTypeSymbol { IsGenericType: true, Name: "NativeHashCollection" } namedType &&
            namedType.ContainingNamespace.ToDisplayString() == "Yooni.Native.Container" &&
            namedType.TypeArguments.Length == 2)
        {
            keyType = namedType.TypeArguments[0];
            valueType = namedType.TypeArguments[1];
            return true;
        }
        
        keyType = null;
        valueType = null;
        return false;
    }
    
    internal static bool IsNativeRingBuffer(
        ITypeSymbol type,
        [NotNullWhen(true)] out ITypeSymbol? itemType,
        [NotNullWhen(true)] out int? size)
    {
        if (type is INamedTypeSymbol { IsGenericType: true, Name: "NativeRingBuffer" } namedType &&
            namedType.ContainingNamespace.ToDisplayString() == "Yooni.Native.Container" &&
            namedType.TypeArguments.Length == 2)
        {
            itemType = namedType.TypeArguments[0];
            var storageType = namedType.TypeArguments[1];
            if (IsNativeStorage(storageType, out var itemType0, out size) && 
                SymbolEqualityComparer.Default.Equals(itemType0, itemType))
                return true;
        }
        
        itemType = null;
        size = null;
        return false;
    }

    internal static bool IsNativeString(
        ITypeSymbol type,
        [NotNullWhen(true)] out int? size)
    {
        for (var candidateSize = 1; candidateSize <= 256; candidateSize *= 2)
        {
            if (type is INamedTypeSymbol { IsGenericType: false } namedType &&
                namedType.Name == $"NativeString{candidateSize}" &&
                namedType.ContainingNamespace.ToDisplayString() == "Yooni.Native.Container")
            {
                size = candidateSize;
                return true;
            }
        }
        
        size = null;
        return false;
    }
    
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