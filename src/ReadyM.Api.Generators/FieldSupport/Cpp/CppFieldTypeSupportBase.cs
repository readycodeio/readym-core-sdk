using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.Cpp;

internal abstract class CppFieldTypeSupportBase : ICppFieldTypeSupport
{
    public abstract bool CanHandle(ITypeSymbol type);

    public virtual string GetCppTypeName(ITypeSymbol type)
        => CppTypeTranslationPipeline.Instance.Translate(type);

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