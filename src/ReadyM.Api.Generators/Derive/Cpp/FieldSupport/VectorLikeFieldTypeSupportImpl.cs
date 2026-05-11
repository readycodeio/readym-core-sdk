using System;
using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCppUtils;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal sealed class VectorLikeFieldTypeSupportImpl : CppNonOverrideFieldTypeSupportImplBase
{
    protected override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsVectorLike(type);

    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        var cppTypeName = CppTypeName(context.State.CurrentType);
        
        context.Append(context.State.CurrentType.Name switch
        {
            "Vector2" => $"{cppTypeName}::DistanceSquared({context.State.CurrentVar}, value) <= {DeriveComponentUtils.VectorComparisonEpsilon}f",
            "Vector3" => $"{cppTypeName}::DistanceSquared({context.State.CurrentVar}, value) <= {DeriveComponentUtils.VectorComparisonEpsilon}f",
            "Vector4" => $"{cppTypeName}::DistanceSquared({context.State.CurrentVar}, value) <= {DeriveComponentUtils.VectorComparisonEpsilon}f",
            _ => throw new InvalidOperationException(
                $"Unsupported vector type: {context.State.CurrentType.ToDisplayString()}")
        });
    }

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        var cppTypeName = CppTypeName(context.State.CurrentType);

        context.Append(context.State.CurrentType.Name switch
        {
            "Vector2" => $"{cppTypeName}::DistanceSquared({context.State.CurrentVar}, value) > {DeriveComponentUtils.VectorComparisonEpsilon}f",
            "Vector3" => $"{cppTypeName}::DistanceSquared({context.State.CurrentVar}, value) > {DeriveComponentUtils.VectorComparisonEpsilon}f",
            "Vector4" => $"{cppTypeName}::DistanceSquared({context.State.CurrentVar}, value) > {DeriveComponentUtils.VectorComparisonEpsilon}f",
            _ => throw new InvalidOperationException(
                $"Unsupported vector type: {context.State.CurrentType.ToDisplayString()}")
        });
    }
}