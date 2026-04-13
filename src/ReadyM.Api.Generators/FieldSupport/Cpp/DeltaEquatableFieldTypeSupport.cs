using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.Cpp;

internal sealed class DeltaEquatableFieldTypeSupport : CppFieldTypeSupportBase
{
    public override bool CanHandle(ITypeSymbol type)
        => SerializationHelper.IsDeltaEquatable(type);

    public override string BuildSetterCondition(DeriveMemberModel model)
        => $"!({model.SourceMember.Name}.DeltaEquals(value, {DeriveUtils.VectorComparisonEpsilon}))";
}