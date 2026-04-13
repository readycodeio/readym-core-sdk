using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.Cpp;

internal sealed class CustomSerializableFieldTypeSupport : CppFieldTypeSupportBase
{
    public override bool CanHandle(ITypeSymbol type)
        => SerializationHelper.HasSerializeMethod(type) && SerializationHelper.HasDeserializeMethod(type);

    public override string BuildSetterCondition(DeriveMemberModel model)
        => $"{model.SourceMember.Name} != value";
}