using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal abstract class CppNonOverrideFieldTypeSupportImplBase : CppFieldTypeSupportImplBase
{
    protected abstract bool Supports(ITypeSymbol type);

    public override bool Supports(DeriveMemberModel model)
    {
        var cppName = AttributeUtils.GetAttribute<string?>(model.Source.Symbol, "CppNativeFieldTypeAttribute", "cppTypeName", null);
        if (cppName != null)
            return false;
        
        return Supports(model.Source.Type);
    }
}