using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.Cpp;

internal sealed class EnumFieldTypeSupport : CppFieldTypeSupportBase
{
    public override bool CanHandle(ITypeSymbol type)
        => type.TypeKind == TypeKind.Enum;

    public override string BuildSetterCondition(DeriveMemberModel model)
        => $"{model.SourceMember.Name} != value";
}