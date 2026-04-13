using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

internal static class DeriveComponentUtils
{
    internal const string FloatComparisonEpsilon = "0.1f";
    internal const string DoubleComparisonEpsilon = "0.1";
    internal const string VectorComparisonEpsilon = "0.01f";

    internal static DeriveMemberInfo GetMemberInfo(ISymbol symbol)
    {
        if (symbol is IFieldSymbol f)
            return new DeriveMemberInfo(
                name: f.Name,
                type: f.Type,
                order: f.DeclaringSyntaxReferences[0].Span.Start,
                readOnly: f.IsReadOnly,
                isInvalid: false);
        else if (symbol is IPropertySymbol p)
            return new DeriveMemberInfo(
                name: p.Name,
                type: p.Type,
                order: p.DeclaringSyntaxReferences[0].Span.Start,
                readOnly: p.SetMethod == null,
                isInvalid: p.GetMethod == null || p.GetMethod?.IsInitOnly == true || p.SetMethod?.IsInitOnly == true);
        else
            throw new InvalidOperationException($"Unsupported symbol type: {symbol.GetType().Name}");
    }

    internal static DeriveTargetInfo GetTargetInfo(
        INamedTypeSymbol symbol,
        bool mapFields,
        bool mapProperties,
        bool mapPrivate,
        bool mapPublic,
        bool mapInternal)
    {
        var ns = symbol.ContainingNamespace.ToDisplayString();
        var name = symbol.Name;

        string? dirtyMaskType = null;
        var errorMessages = new List<string>();
        var allMembers = new List<DeriveMemberInfo>();
        
        foreach (var member in symbol.GetMembers())
        {
            bool isField;
            var useMember = true;
            var canUseMember = true;

            if (member.Name == "_dirtyMask")
            {
                dirtyMaskType = member switch
                {
                    IFieldSymbol maskField => maskField.Type.ToDisplayString(),
                    IPropertySymbol propField => propField.Type.ToDisplayString(),
                    _ => throw new InvalidOperationException($"Unsupported symbol type for dirty mask: {member.GetType().Name}")
                };
                continue;
            }
            
            if (member.DeclaredAccessibility == Accessibility.Private)
            {
                if (!mapPrivate)
                    useMember = false;
            }
            else if (member.DeclaredAccessibility == Accessibility.Public)
            {
                if (!mapPublic)
                    useMember = false;
            }
            else if (member.DeclaredAccessibility == Accessibility.Internal)
            {
                if (!mapInternal)
                    useMember = false;
            }
            else
            {
                useMember = false;
                canUseMember = false;
            }

            if (member.DeclaringSyntaxReferences.Length <= 0)
            {
                useMember = false;
                canUseMember = false;
            }
            
            if (member is IFieldSymbol f)
            {
                if (!mapFields)
                    useMember = false;

                if (f is { IsStatic: true })
                {
                    // Static readonly fields are not serialized
                    useMember = false;
                    canUseMember = false;
                }
                
                if (f is { IsReadOnly: true })
                {
                    canUseMember = false;
                }

                isField = true;
            }
            else if (member is IPropertySymbol p)
            {
                if (!mapProperties)
                    useMember = false;
                
                if (p is { IsStatic: true })
                {
                    useMember = false;
                    canUseMember = false;
                }
                
                if (p is not { GetMethod: not null, SetMethod: not null })
                {
                    canUseMember = false;
                }

                isField = false;
            }
            else
            {
                useMember = false;
                canUseMember = false;
                isField = false;
            }

            var hasExclude = member.GetAttributes().Any(a => a.AttributeConstructor?.Name == "ExcludeSerializable");
            var hasInclude = member.GetAttributes().Any(a => a.AttributeConstructor?.Name == "IncludeSerializable");

            if (hasInclude && hasExclude)
            {
                errorMessages.Add($"Cannot have `IncludeSerializable` and `ExcludeSerializable` on the same {(isField ? "field" : "property")}: {member.Name}");
                continue;
            }

            if (hasInclude)
                useMember = true;
            if (hasExclude)
                useMember = false;

            if (!canUseMember && useMember)
            {
                errorMessages.Add($"Cannot use {(isField ? "field" : "property")}: {member.Name}");
                continue;
            }

            if (useMember)
            {
                var fieldInfo = GetMemberInfo(member);
                allMembers.Add(fieldInfo);
            }
        }

        var thisNullable = symbol.IsReferenceType && symbol.NullableAnnotation != NullableAnnotation.Annotated;

        return new DeriveTargetInfo(
            name: name,
            @namespace: ns,
            members: allMembers.ToArray(),
            isNullable: thisNullable,
            errorMessages: errorMessages.ToArray(),
            dirtyMaskType: dirtyMaskType
        );
    }

    internal static DeriveTargetGenerationModel CreateGenerationModel(
        INamedTypeSymbol symbol,
        bool mapFields,
        bool mapProperties,
        bool mapPrivate,
        bool mapPublic,
        bool mapInternal,
        bool emitDirtyMask)
    {
        if (symbol == null)
            throw new ArgumentNullException(nameof(symbol));

        var targetInfo = GetTargetInfo(
            symbol,
            mapFields: mapFields,
            mapProperties: mapProperties,
            mapPrivate: mapPrivate,
            mapPublic: mapPublic,
            mapInternal: mapInternal);

        if (targetInfo == null)
            throw new InvalidOperationException("GeneratorHelper.GetSymbolInfo returned null.");

        var mask = ResolveMaskInfo(targetInfo.Members.Length, targetInfo.DirtyMaskType, emitDirtyMask);

        var members = new DeriveMemberGenerationModel[targetInfo.Members.Length];
        for (var i = 0; i < targetInfo.Members.Length; i++)
        {
            var member = targetInfo.Members[i];
            var type = member.Type;
            if (type == null)
                throw new InvalidOperationException("Member type unexpectedly null.");

            var usePutGet = SerializationHelper.IsSerializablePrimitive(type.SpecialType);
            var isEnum = type.TypeKind == TypeKind.Enum;
            var isEquatable = SerializationHelper.IsEquatable(type);
            var isDeltaEquatable = SerializationHelper.IsDeltaEquatable(type);
            var isCustomSerializable = HasSerializeMethod(type) && HasDeserializeMethod(type);
            var isVectorLike = IsVectorLike(type);

            var isSupported =
                usePutGet ||
                isEnum ||
                isEquatable ||
                isDeltaEquatable ||
                isCustomSerializable ||
                isVectorLike;

            if (member.IsInvalid)
                isSupported = false;

            var enumBaseType = isEnum
                ? SerializationHelper.GetEnumBaseType(type)
                : SpecialType.None;

            members[i] = new DeriveMemberGenerationModel(
                member,
                i,
                GetGeneratedPropertyName(member.Name),
                isSupported,
                usePutGet,
                isEnum,
                enumBaseType,
                isEquatable,
                isDeltaEquatable,
                isCustomSerializable,
                isVectorLike);
        }

        return new DeriveTargetGenerationModel(targetInfo, mask, members, emitDirtyMask);
    }

    internal static DeriveMaskInfo ResolveMaskInfo(int memberCount, string? requestedMaskType, bool emitDirtyMask)
    {
        string maskType;
        var invalid = false;

        if (!emitDirtyMask)
        {
            if (string.IsNullOrWhiteSpace(requestedMaskType))
            {
                maskType = "ulong";
                invalid = true;
            }
            else
            {
                maskType = requestedMaskType!;
            }
        }
        else
        {
            if (memberCount <= sizeof(byte) * 8)
                maskType = "byte";
            else if (memberCount <= sizeof(ushort) * 8)
                maskType = "ushort";
            else if (memberCount <= sizeof(uint) * 8)
                maskType = "uint";
            else if (memberCount <= sizeof(ulong) * 8)
                maskType = "ulong";
            else
            {
                maskType = "ulong";
                invalid = true;
            }
        }

        int bits;
        string readMethod;
        string cppType;

        switch (maskType)
        {
            case "byte":
                bits = sizeof(byte) * 8;
                readMethod = "GetByte";
                cppType = "uint8_t";
                break;
            case "ushort":
                bits = sizeof(ushort) * 8;
                readMethod = "GetUShort";
                cppType = "uint16_t";
                break;
            case "uint":
                bits = sizeof(uint) * 8;
                readMethod = "GetUInt";
                cppType = "uint32_t";
                break;
            case "ulong":
                bits = sizeof(ulong) * 8;
                readMethod = "GetULong";
                cppType = "uint64_t";
                break;
            default:
                bits = sizeof(ulong) * 8;
                readMethod = "GetULong";
                cppType = "uint64_t";
                invalid = true;
                break;
        }

        if (bits < memberCount)
            invalid = true;

        return new DeriveMaskInfo(maskType, cppType, readMethod, bits, invalid);
    }

    internal static IEnumerable<string> GetUserInputErrors(DeriveTargetGenerationModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        foreach (var error in model.TargetInfo.ErrorMessages)
            yield return error;

        if (model.Mask.Invalid)
        {
            if (model.EmitDirtyMask)
            {
                yield return "Too many networked members in '" + model.TargetInfo.Name +
                             "' to fit in a dirty mask. Maximum supported is " + model.Mask.Bits.ToString(CultureInfo.InvariantCulture) +
                             ", but " + model.Members.Length.ToString(CultureInfo.InvariantCulture) + " were found.";
            }
            else
            {
                yield return "Too many networked members in '" + model.TargetInfo.Name +
                             "' to fit in the specified _dirtyMask. Maximum supported is " + model.Mask.Bits.ToString(CultureInfo.InvariantCulture) +
                             ", but " + model.Members.Length.ToString(CultureInfo.InvariantCulture) + " were found.";
            }
        }

        foreach (var member in model.Members)
        {
            if (!member.IsSupported)
            {
                yield return "Unsupported type '" + member.Member.Type.ToDisplayString() +
                             "' for networked member '" + member.Member.Name + "'.";
            }
        }
    }

    internal static string GetGeneratedPropertyName(string memberName)
    {
        if (string.IsNullOrEmpty(memberName))
            throw new ArgumentException("Member name must not be null or empty.", nameof(memberName));

        if (memberName.StartsWith("_", StringComparison.Ordinal))
        {
            if (memberName.Length == 1)
                return "EmptyNameField";

            return char.ToUpperInvariant(memberName[1]) + memberName.Substring(2);
        }

        if (char.IsUpper(memberName[0]))
            return memberName + "DirtyAware";

        return char.ToUpperInvariant(memberName[0]) + memberName.Substring(1);
    }

    internal static string GetCppTypeName(ITypeSymbol type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (type.TypeKind == TypeKind.Enum)
            return type.Name;

        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean: return "bool";
            case SpecialType.System_Byte: return "uint8_t";
            case SpecialType.System_SByte: return "int8_t";
            case SpecialType.System_Int16: return "int16_t";
            case SpecialType.System_UInt16: return "uint16_t";
            case SpecialType.System_Int32: return "int32_t";
            case SpecialType.System_UInt32: return "uint32_t";
            case SpecialType.System_Int64: return "int64_t";
            case SpecialType.System_UInt64: return "uint64_t";
            case SpecialType.System_Single: return "float";
            case SpecialType.System_Double: return "double";
            case SpecialType.System_Char: return "char16_t";
        }

        if (IsVectorLike(type))
            return type.Name;

        return type.Name;
    }

    internal static string GetCppDefaultValue(ITypeSymbol type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (type.TypeKind == TypeKind.Enum)
            return "{}";

        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean: return "false";
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
                return "0";
            case SpecialType.System_Single:
                return "0.0f";
            case SpecialType.System_Double:
                return "0.0";
            case SpecialType.System_Char:
                return "0";
        }

        if (type.IsReferenceType)
            return "nullptr";

        return "{}";
    }

    internal static string BuildCppSetterCondition(DeriveMemberGenerationModel member)
    {
        if (member == null)
            throw new ArgumentNullException(nameof(member));

        var fieldName = member.Member.Name;
        var type = member.Member.Type;

        if (type == null)
            throw new InvalidOperationException("Member type unexpectedly null.");

        if (!member.IsSupported)
            return "true";

        if (type.SpecialType == SpecialType.System_Single)
            return "std::abs(" + fieldName + " - value) > " + FloatComparisonEpsilon;

        if (type.SpecialType == SpecialType.System_Double)
            return "std::abs(" + fieldName + " - value) > " + DoubleComparisonEpsilon;

        if (type.Name == "Vector2")
            return "Vector2::DistanceSquared(" + fieldName + ", value) > " + VectorComparisonEpsilon;

        if (type.Name == "Vector3")
            return "Vector3::DistanceSquared(" + fieldName + ", value) > " + VectorComparisonEpsilon;

        if (type.Name == "Vector4")
            return "Vector4::DistanceSquared(" + fieldName + ", value) > " + VectorComparisonEpsilon;

        if (member.IsDeltaEquatable)
            return "!(" + fieldName + ".DeltaEquals(value, " + VectorComparisonEpsilon + "))";

        if (member.IsEquatable || member.IsEnum || member.UsePutGet)
            return fieldName + " != value";

        return fieldName + " != value";
    }

    private static bool HasSerializeMethod(ITypeSymbol type)
        => type.GetMembers("Serialize")
            .OfType<IMethodSymbol>()
            .Any(m =>
                m.Parameters.Length == 1 &&
                m.Parameters[0].Type.ToDisplayString() == "LiteNetLib.Utils.NetDataWriter");

    private static bool HasDeserializeMethod(ITypeSymbol type)
        => type.GetMembers("Deserialize")
            .OfType<IMethodSymbol>()
            .Any(m =>
                m.Parameters.Length == 1 &&
                m.Parameters[0].Type.ToDisplayString() == "LiteNetLib.Utils.NetDataReader");

    private static bool IsVectorLike(ITypeSymbol type)
        => (type.Name is "Vector2" or "Vector3" or "Vector4") &&
           type.ContainingNamespace.ToDisplayString() == "System.Numerics";

    public static string GetGeneratedFileName(INamedTypeSymbol symbol)
        => symbol.ContainingNamespace != null ? $"{symbol.ContainingNamespace.ToDisplayString()}.{symbol.Name}" : symbol.Name;
}