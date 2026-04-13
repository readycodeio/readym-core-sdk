using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.Cpp;

internal sealed class EquatableFieldTypeSupport : CppFieldTypeSupportBase
{
    public override bool CanHandle(ITypeSymbol type)
        => SerializationHelper.IsEquatable(type);

    public override string BuildSetterCondition(DeriveMemberModel model)
        => $"{model.SourceMember.Name} != value";
}