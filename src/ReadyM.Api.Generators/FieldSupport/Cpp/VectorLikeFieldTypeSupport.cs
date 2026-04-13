using System;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.FieldSupport.Cpp;

internal sealed class VectorLikeFieldTypeSupport : CppFieldTypeSupportBase
{
    public override bool CanHandle(ITypeSymbol type)
        => SerializationHelper.IsVectorLike(type);

    public override string BuildSetterCondition(DeriveMemberModel model)
        => model.SourceMember.Type.Name switch
        {
            "Vector2" => $"{GetCppTypeName(model.SourceMember.Type)}::DistanceSquared({model.SourceMember.Name}, value) > {DeriveComponentUtils.VectorComparisonEpsilon}f",
            "Vector3" => $"{GetCppTypeName(model.SourceMember.Type)}::DistanceSquared({model.SourceMember.Name}, value) > {DeriveComponentUtils.VectorComparisonEpsilon}f",
            "Vector4" => $"{GetCppTypeName(model.SourceMember.Type)}::DistanceSquared({model.SourceMember.Name}, value) > {DeriveComponentUtils.VectorComparisonEpsilon}f",
            _ => throw new InvalidOperationException($"Unsupported vector type: {model.SourceMember.Type.ToDisplayString()}")
        };
}