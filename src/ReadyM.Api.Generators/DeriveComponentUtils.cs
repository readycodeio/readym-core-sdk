using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.FieldSupport;

namespace ReadyM.Api.Generators;

public class DeriveComponentUtils
{
    internal const float FloatComparisonEpsilon = 0.1f;
    internal const double DoubleComparisonEpsilon = 0.1;

    internal static float Vector2ComparisonEpsilon = FloatComparisonEpsilon * FloatComparisonEpsilon;
    internal static float Vector3ComparisonEpsilon = FloatComparisonEpsilon * FloatComparisonEpsilon * FloatComparisonEpsilon;
    internal static float Vector4ComparisonEpsilon = FloatComparisonEpsilon * FloatComparisonEpsilon * FloatComparisonEpsilon * FloatComparisonEpsilon;

    internal static DeriveTargetModel GetTargetModel(INamedTypeSymbol symbol)
    {
        var mode = AttributeUtils.GetAttribute<byte>(
            symbol,
            "DeriveINetworkedComponentAttribute",
            "mode",
            (1 << 0) | (1 << 2));
        var mapSettings = DeriveUtils.GetMapSettings(mode);

        var emitDirtyMask = AttributeUtils.GetAttribute(
            symbol,
            "DeriveINetworkedComponentAttribute",
            "emitDirtyMask",
            true);
        
        var targetInfo = DeriveUtils.GetTargetInfo(symbol, emitDirtyMask, mapSettings);
        var mask = GetMaskInfo(targetInfo);
        var members = GetMemberModelList(targetInfo);

        return new DeriveTargetModel(targetInfo, mask, members);
    }

    internal static DeriveMaskInfo GetMaskInfo(DeriveTargetInfo targetInfo)
    {
        if (targetInfo == null)
            throw new ArgumentNullException(nameof(targetInfo));

        return ResolveMaskInfo(
            targetInfo.Members.Length,
            targetInfo.DirtyMaskType,
            targetInfo.EmitDirtyMask);
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

    
    internal static DeriveMemberModelWithSupport[] GetMemberModelList(DeriveTargetInfo targetInfo)
    {
        var members = new DeriveMemberModelWithSupport[targetInfo.Members.Length];

        for (var i = 0; i < targetInfo.Members.Length; i++)
        {
            var memberModel = GetMemberModel(targetInfo.Members[i], i);
            members[i] = memberModel;
        }

        return members;
    }
    
    internal static DeriveMemberModelWithSupport GetMemberModel(DeriveMemberInfo memberInfo, int index)
    {
        var type = memberInfo.Type ?? throw new InvalidOperationException("Member type unexpectedly null.");

        var csharpSupport = memberInfo.IsInvalid ? null : FieldSupportRegistry.ResolveCSharpSupport(type);
        var cppSupport = memberInfo.IsInvalid ? null : FieldSupportRegistry.ResolveCppSupport(type);

        return new DeriveMemberModelWithSupport(
            new DeriveMemberModel(memberInfo, GetGeneratedPropertyName(memberInfo.Name), index),
            csharpSupport,
            cppSupport);
    }
    
    internal static IEnumerable<string> GetUserInputErrors(
        DeriveTargetInfo targetInfo,
        DeriveMaskInfo mask,
        DeriveMemberModelWithSupport[] members,
        bool emitDirtyMask,
        bool forCpp)
    {
        foreach (var error in targetInfo.ErrorMessages)
            yield return error;

        if (mask.Invalid)
        {
            if (emitDirtyMask)
            {
                yield return "Too many networked members in '" + targetInfo.Name +
                             "' to fit in a dirty mask. Maximum supported is " + mask.Bits.ToString(CultureInfo.InvariantCulture) +
                             ", but " + members.Length.ToString(CultureInfo.InvariantCulture) + " were found.";
            }
            else
            {
                yield return "Too many networked members in '" + targetInfo.Name +
                             "' to fit in the specified _dirtyMask. Maximum supported is " + mask.Bits.ToString(CultureInfo.InvariantCulture) +
                             ", but " + members.Length.ToString(CultureInfo.InvariantCulture) + " were found.";
            }
        }

        foreach (var member in members)
        {
            var supported = forCpp ? member.IsCppSupported : member.IsCSharpSupported;
            if (!supported)
            {
                yield return "Unsupported type '" + member.Model.SourceMember.Type.ToDisplayString() +
                             "' for networked member '" + member.Model.SourceMember.Name + "'.";
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
}