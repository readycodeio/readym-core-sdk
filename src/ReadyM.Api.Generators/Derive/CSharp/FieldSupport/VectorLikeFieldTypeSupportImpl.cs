using System;
using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal sealed class VectorLikeFieldTypeSupportImpl : CSharpFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsVectorLike(type);

    protected override void EmitEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.Append(
            symbol.Name switch
            {
                "Vector2" => $"{FullyQualifiedTypeName(context.State.CurrentType)}.DistanceSquared({context.State.CurrentVar}, value) <= {DeriveComponentUtils.VectorComparisonEpsilon}f",
                "Vector3" => $"{FullyQualifiedTypeName(context.State.CurrentType)}.DistanceSquared({context.State.CurrentVar}, value) <= {DeriveComponentUtils.VectorComparisonEpsilon}f",
                "Vector4" => $"{FullyQualifiedTypeName(context.State.CurrentType)}.DistanceSquared({context.State.CurrentVar}, value) <= {DeriveComponentUtils.VectorComparisonEpsilon}f",
                _ => throw new InvalidOperationException($"Unsupported vector type: {symbol.ToDisplayString()}"),
            });

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.Append(
            symbol.Name switch
            {
                "Vector2" => $"{FullyQualifiedTypeName(context.State.CurrentType)}.DistanceSquared({context.State.CurrentVar}, value) > {DeriveComponentUtils.VectorComparisonEpsilon}f",
                "Vector3" => $"{FullyQualifiedTypeName(context.State.CurrentType)}.DistanceSquared({context.State.CurrentVar}, value) > {DeriveComponentUtils.VectorComparisonEpsilon}f",
                "Vector4" => $"{FullyQualifiedTypeName(context.State.CurrentType)}.DistanceSquared({context.State.CurrentVar}, value) > {DeriveComponentUtils.VectorComparisonEpsilon}f",
                _ => throw new InvalidOperationException($"Unsupported vector type: {symbol.ToDisplayString()}"),
            });
}