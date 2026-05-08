using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

public class DeriveComponentUtils
{
    internal const string ScalarComparisonEpsilon = "0.1";
    internal const string VectorComparisonEpsilon = "0.01";

    internal static DeriveTargetModel GetTargetModel(INamedTypeSymbol symbol, GeneratorSyntaxContext context)
    {
        var isNetComponent = AttributeUtils.HasAttribute(symbol, "DeriveINetworkedComponentAttribute");

        byte mode;
        if (AttributeUtils.HasAttribute(symbol, "DeriveINetworkedComponentAttribute"))
        {
            mode = AttributeUtils.GetAttribute<byte>(
                symbol,
                "DeriveINetworkedComponentAttribute",
                "mode",
                (1 << 0) | (1 << 2));
        }
        else if (AttributeUtils.HasAttribute(symbol, "DeriveINetSerializableAttribute"))
        {
            mode = AttributeUtils.GetAttribute<byte>(
                symbol,
                "DeriveINetSerializableAttribute",
                "mode",
                (1 << 0) | (1 << 2));
        }
        else
        {
            mode = (1 << 0) | (1 << 2);
        }
        
        var mapSettings = DeriveUtils.GetMapSettings(mode);

        var emitDirtyMask = false;
        if (AttributeUtils.HasAttribute(symbol, "DeriveINetworkedComponentAttribute"))
        {
            emitDirtyMask = AttributeUtils.GetAttribute(
                symbol,
                "DeriveINetworkedComponentAttribute",
                "emitDirtyMask",
                true);
        }
        
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
        var errors = new List<string>();
        
        ITypeSymbol maskType;

        if (!emitDirtyMask)
        {
            if (requestedMaskType == null)
            {
                maskType = context.SemanticModel.Compilation.GetSpecialType(SpecialType.System_UInt64);
            }
            else
            {
                maskType = requestedMaskType;
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
                errors.Add($"The number of members ({memberCount}) exceeds the maximum supported by the largest dirty mask type (64 bits).");
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
                errors.Add($"The specified dirty mask type '{maskType.ToDisplayString()}' is not a supported integral type. Supported types are byte, ushort, uint, and ulong.");
                break;
        }

        if (bits < memberCount)
        {
            errors.Add($"The number of members ({memberCount}) exceeds the number of bits in the dirty mask type ({bits}).");
        }

        return new DeriveMaskInfo(maskType, bits, errors);
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
    {
        var generatedPropertyName = GetGeneratedPropertyName(memberInfo.Name);
        var skipAccessors = AttributeUtils.HasAttribute(memberInfo.Symbol, "SkipNativeAccessMethodsAttribute");
        var boolAccessors = AttributeUtils.HasAttribute(memberInfo.Symbol, "BoolNativeAccessMethodsAttribute");
        var settings = new DeriveAccessorMemberSettings(
            skipAccessors: skipAccessors,
            boolAccessors: boolAccessors);
        
        return new DeriveMemberModel(
            source: memberInfo, 
            generatedPropertyName: generatedPropertyName,
            maskIndex: index,
            settings: settings);
    }

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