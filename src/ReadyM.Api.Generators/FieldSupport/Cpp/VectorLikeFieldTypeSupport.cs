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
            "Vector2" => $"Vector2::DistanceSquared({model.SourceMember.Name}, value) > {DeriveComponentUtils.Vector2ComparisonEpsilon}f",
            "Vector3" => $"Vector3::DistanceSquared({model.SourceMember.Name}, value) > {DeriveComponentUtils.Vector3ComparisonEpsilon}f",
            "Vector4" => $"Vector4::DistanceSquared({model.SourceMember.Name}, value) > {DeriveComponentUtils.Vector4ComparisonEpsilon}f",
            _ => throw new InvalidOperationException($"Unsupported vector type: {model.SourceMember.Type.ToDisplayString()}")
        };
}