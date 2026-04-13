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
            "Vector2" => $"Vector2::DistanceSquared({model.SourceMember.Name}, value) > {DeriveUtils.VectorComparisonEpsilon}",
            "Vector3" => $"Vector3::DistanceSquared({model.SourceMember.Name}, value) > {DeriveUtils.VectorComparisonEpsilon}",
            "Vector4" => $"Vector4::DistanceSquared({model.SourceMember.Name}, value) > {DeriveUtils.VectorComparisonEpsilon}",
            _ => throw new InvalidOperationException($"Unsupported vector type: {model.SourceMember.Type.ToDisplayString()}")
        };
}