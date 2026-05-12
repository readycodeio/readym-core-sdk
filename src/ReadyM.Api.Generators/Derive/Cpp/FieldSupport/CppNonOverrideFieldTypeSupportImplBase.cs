using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal abstract class CppNonOverrideFieldTypeSupportImplBase : CppFieldTypeSupportImplBase
{
    protected abstract bool Supports(ITypeSymbol type);

    public override bool Supports(DeriveMemberModel model)
    {
        if (!string.IsNullOrEmpty(model.CppSettings.CppTypeName))
            return false;
        
        return Supports(model.Source.Type);
    }
}