using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.Cpp;

internal sealed class PrimitiveFieldTypeSupport : CppFieldTypeSupportBase
{
    public override bool CanHandle(ITypeSymbol type)
        => SerializationHelper.IsSerializablePrimitive(type.SpecialType);

    public override string BuildSetterCondition(DeriveMemberModel model)
    {
        var fieldName = model.SourceMember.Name;
        var type = model.SourceMember.Type;

        if (type.SpecialType == SpecialType.System_Single)
            return $"std::abs({fieldName} - value) > {DeriveComponentUtils.FloatComparisonEpsilon}f";

        if (type.SpecialType == SpecialType.System_Double)
            return $"std::abs({fieldName} - value) > {DeriveComponentUtils.DoubleComparisonEpsilon}";

        return $"{fieldName} != value";
    }
}