using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.Cpp;

internal class NativeContainerFieldTypeSupport : CppFieldTypeSupportBase
{
    public override bool CanHandle(ITypeSymbol type)
        => SerializationHelper.IsNativeContainer(type);

    public override string BuildSetterCondition(DeriveMemberModel model)
        => $"{model.SourceMember.Name} != value";
}