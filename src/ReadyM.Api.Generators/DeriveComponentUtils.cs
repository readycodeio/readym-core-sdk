using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

public static class DeriveComponentUtils
{
    internal const string ScalarComparisonEpsilon = "0.1";
    internal const string VectorComparisonEpsilon = "0.01";

    internal static DeriveTargetModel GetTargetModel(
        INamedTypeSymbol symbol,
        GeneratorSyntaxContext context,
        IReadOnlyList<AttributeData>? fieldAttributes = null)
    {
        return GetTargetModel(symbol, context, null, fieldAttributes);
    }

    internal static DeriveTargetModel GetTargetModel(
        INamedTypeSymbol symbol,
        AttributeData? nativeComponentAttribute,
        IReadOnlyList<AttributeData>? fieldAttributes)
    {
        return GetTargetModel(symbol, null, nativeComponentAttribute, fieldAttributes);
    }

    private static DeriveTargetModel GetTargetModel(
        INamedTypeSymbol symbol,
        GeneratorSyntaxContext? context,
        AttributeData? nativeComponentAttribute,
        IReadOnlyList<AttributeData>? fieldAttributes)
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

        var emitBindDelete = false;
            
        if (nativeComponentAttribute != null)
        {
            emitBindDelete = AttributeUtils.GetAttributeValue<bool>(
                nativeComponentAttribute,
                "bindDelete",
                false);
        }
        else if (AttributeUtils.HasAttribute(symbol, "NativeComponentAttribute"))
        {
            emitBindDelete = AttributeUtils.GetAttribute<bool>(
                symbol,
                "NativeComponentAttribute",
                "bindDelete",
                false);
        }
        
        var targetInfo = DeriveUtils.GetTargetInfo(symbol, emitDirtyMask, emitBindDelete, mapSettings);
        ExtendTargetInfo(targetInfo, fieldAttributes);
        var members = GetMemberModelList(targetInfo, fieldAttributes);

        DeriveMaskInfo? mask = null;
        if (isNetComponent)
        {
            if (context == null)
                throw new InvalidOperationException("GeneratorSyntaxContext is required to derive dirty mask information.");

            mask = GetMaskInfo(targetInfo, context.Value);
        }
        
        return new DeriveTargetModel(targetInfo, members, mask);
    }

    private static void ExtendTargetInfo(DeriveTargetInfo targetInfo, IReadOnlyList<AttributeData>? fieldAttributes)
    {
        if (fieldAttributes == null)
            return;
        
        var newMembers = new List<MemberInfo>();
        foreach (var fieldAttribute in fieldAttributes)
        {
            var forField = AttributeUtils.GetAttributeValue<string?>(fieldAttribute, "forField", null);
            if (string.IsNullOrEmpty(forField))
                continue;

            if (forField == null)
            {
                targetInfo.AddError($"Field attribute '{fieldAttribute.AttributeClass?.Name}' is missing the required 'forField' argument.");
                continue;   
            }
            
            foreach (var member in targetInfo.Members)
            {
                if (member.Name == forField)
                    goto end;
            }
            
            var fieldType = AttributeUtils.GetAttributeValue<INamedTypeSymbol?>(fieldAttribute, "fieldType", null);
            if (fieldType == null)
            {
                targetInfo.AddError($"Field attribute '{fieldAttribute.AttributeClass?.Name}' is missing the required 'fieldType' argument.");
                continue;
            }

            var readOnly = AttributeUtils.GetAttributeValue<bool>(fieldAttribute, "readOnly", false);
            
            targetInfo.AddMember(new DeriveMemberInfo(
                symbol: new DummySymbol(forField!, targetInfo.Symbol, null, Accessibility.Private),
                name: forField!,
                type: fieldType,
                order: 99999,
                readOnly: readOnly,
                errors: []));
            
            end: {}
        }
    }

    private static DeriveMaskInfo GetMaskInfo(DeriveTargetInfo targetInfo, GeneratorSyntaxContext context)
    {
        var memberCount = targetInfo.Members.Count;
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
    
    private static DeriveMemberModel[] GetMemberModelList(
        DeriveTargetInfo targetInfo,
        IReadOnlyList<AttributeData>? fieldAttributes)
    {
        var members = new DeriveMemberModel[targetInfo.Members.Count];

        for (var i = 0; i < targetInfo.Members.Count; i++)
        {
            var memberModel = GetMemberModel(targetInfo.Members[i], i, fieldAttributes);
            members[i] = memberModel;
        }

        return members;
    }
    
    private static DeriveMemberModel GetMemberModel(
        DeriveMemberInfo memberInfo,
        int index,
        IReadOnlyList<AttributeData>? fieldAttributes)
    {
        var generatedPropertyName = GetGeneratedPropertyName(memberInfo.Name);
        var skipAccessors =
            AttributeUtils.HasAttribute(memberInfo.Symbol, "SkipNativeAccessMethodsAttribute") ||
            HasFieldAttribute(fieldAttributes, memberInfo.Name, "SkipNativeAccessMethodsForAttribute");
        var boolAccessors =
            AttributeUtils.HasAttribute(memberInfo.Symbol, "BoolNativeAccessMethodsAttribute") ||
            HasFieldAttribute(fieldAttributes, memberInfo.Name, "BoolNativeAccessMethodsForAttribute");
        var settings = new DeriveAccessorMemberSettings(
            skipAccessors: skipAccessors,
            boolAccessors: boolAccessors);

        var cppSettings = GetCppSettings(memberInfo, fieldAttributes);
        
        return new DeriveMemberModel(
            source: memberInfo, 
            generatedPropertyName: generatedPropertyName,
            maskIndex: index,
            accessorSettings: settings,
            cppSettings: cppSettings);
    }
    
    private static DeriveMemberCppSettings GetCppSettings(
        DeriveMemberInfo memberInfo,
        IReadOnlyList<AttributeData>? fieldAttributes)
    {
        var cppTypeName = AttributeUtils.GetAttribute<string?>(memberInfo.Symbol, "CppNativeFieldTypeAttribute", "cppTypeName", null)
            ?? GetFieldAttribute<string?>(fieldAttributes, memberInfo.Name, "CppNativeFieldTypeForAttribute", "cppTypeName", null);
        var defaultValue = AttributeUtils.GetAttribute<string?>(memberInfo.Symbol, "CppNativeFieldTypeAttribute", "defaultValue", "{}")
            ?? GetFieldAttribute<string?>(fieldAttributes, memberInfo.Name, "CppNativeFieldTypeForAttribute", "defaultValue", "{}");
        var getterTypeName = AttributeUtils.GetAttribute<string?>(memberInfo.Symbol, "CppNativeFieldTypeAttribute", "getterTypeName", null)
            ?? GetFieldAttribute<string?>(fieldAttributes, memberInfo.Name, "CppNativeFieldTypeForAttribute", "getterTypeName", null);
        var setterTypeName = AttributeUtils.GetAttribute<string?>(memberInfo.Symbol, "CppNativeFieldTypeAttribute", "setterTypeName", null)
            ?? GetFieldAttribute<string?>(fieldAttributes, memberInfo.Name, "CppNativeFieldTypeForAttribute", "setterTypeName", null);
        var useMove = AttributeUtils.GetAttribute<bool>(memberInfo.Symbol, "CppNativeFieldTypeAttribute", "useMove", false)
            || GetFieldAttribute<bool>(fieldAttributes, memberInfo.Name, "CppNativeFieldTypeForAttribute", "useMove", false);
        var includes = AttributeUtils.GetArrayAttribute<string>(memberInfo.Symbol, "CppNativeFieldTypeAttribute", "includes");
        if (includes == null || includes.Count == 0)
            includes = GetFieldAttribute<IReadOnlyList<string>>(fieldAttributes, memberInfo.Name, "CppNativeFieldTypeForAttribute", "includes", []);
        
        return new DeriveMemberCppSettings(
            cppTypeName: cppTypeName,
            defaultValue: defaultValue,
            getterTypeName: getterTypeName,
            setterTypeName: setterTypeName,
            useMove: useMove,
            includes: includes);
    }

    private static bool HasFieldAttribute(
        IReadOnlyList<AttributeData>? fieldAttributes,
        string fieldName,
        string attrName)
    {
        if (fieldAttributes == null)
            return false;

        foreach (var attr in fieldAttributes)
        {
            if (!AttributeUtils.IsAttribute(attr, attrName))
                continue;

            var forField = AttributeUtils.GetAttributeValue<string?>(
                attr,
                "forField",
                null);

            if (string.Equals(forField, fieldName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static T GetFieldAttribute<T>(IReadOnlyList<AttributeData>? fieldAttributes,
        string fieldName,
        string attrName,
        string keyName,
        T defaultValue)
    {
        if (fieldAttributes == null)
            return defaultValue;
        foreach (var attr in fieldAttributes)
        {
            if (!AttributeUtils.IsAttribute(attr, attrName))
                continue;
            
            var forField = AttributeUtils.GetAttributeValue<string?>(
                attr,
                "forField",
                null);
            if (string.Equals(forField, fieldName, StringComparison.Ordinal))
            {
                return AttributeUtils.GetAttributeValue<T>(
                    attr,
                    keyName,
                    defaultValue);
            }
        }
        
        return defaultValue;
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