using System;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

public class DeriveComponentUtils
{
    internal const float FloatComparisonEpsilon = 0.1f;
    internal const double DoubleComparisonEpsilon = 0.1;

    internal static readonly float VectorComparisonEpsilon = FloatComparisonEpsilon * FloatComparisonEpsilon;

    internal static DeriveTargetModel GetTargetModel(INamedTypeSymbol symbol, GeneratorSyntaxContext context)
    {
        var isNetComponent = AttributeUtils.HasAttribute(symbol, "DeriveINetworkedComponentAttribute");
        
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
        var members = GetMemberModelList(targetInfo);

        DeriveMaskInfo? mask = null;
        if (isNetComponent)
            mask = GetMaskInfo(targetInfo, context);
        
        return new DeriveTargetModel(targetInfo, members, mask);
    }
    
    private static DeriveMaskInfo GetMaskInfo(DeriveTargetInfo targetInfo, GeneratorSyntaxContext context)
    {
        var memberCount = targetInfo.Members.Length;
        var requestedMaskType = targetInfo.RequestedDirtyMaskType;
        var emitDirtyMask = targetInfo.EmitDirtyMask;
        
        ITypeSymbol maskType;
        var invalid = false;

        if (!emitDirtyMask)
        {
            if (requestedMaskType == null)
            {
                maskType = context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_UInt64);
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
                maskType = context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_Byte);
            else if (memberCount <= sizeof(ushort) * 8)
                maskType = context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_UInt16);
            else if (memberCount <= sizeof(uint) * 8)
                maskType = context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_UInt32);
            else if (memberCount <= sizeof(ulong) * 8)
                maskType = context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_UInt64);
            else
            {
                maskType = context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_UInt64);
                invalid = true;
            }
        }

        int bits;

        switch (maskType.SpecialType)
        {
            case SpecialType.System_Byte:
                bits = sizeof(byte) * 8;
                break;
            case SpecialType.System_UInt16:
                bits = sizeof(ushort) * 8;
                break;
            case SpecialType.System_UInt32:
                bits = sizeof(uint) * 8;
                break;
            case SpecialType.System_UInt64:
                bits = sizeof(ulong) * 8;
                break;
            default:
                bits = sizeof(ulong) * 8;
                invalid = true;
                break;
        }

        if (bits < memberCount)
            invalid = true;

        return new DeriveMaskInfo(maskType, bits, invalid);
    }

    
    private static DeriveMemberModel[] GetMemberModelList(DeriveTargetInfo targetInfo)
    {
        var members = new DeriveMemberModel[targetInfo.Members.Length];

        for (var i = 0; i < targetInfo.Members.Length; i++)
        {
            var memberModel = GetMemberModel(targetInfo.Members[i], i);
            members[i] = memberModel;
        }

        return members;
    }
    
    private static DeriveMemberModel GetMemberModel(DeriveMemberInfo memberInfo, int index)
        => new(memberInfo, GetGeneratedPropertyName(memberInfo.Name), index);
    
    private static string GetGeneratedPropertyName(string memberName)
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