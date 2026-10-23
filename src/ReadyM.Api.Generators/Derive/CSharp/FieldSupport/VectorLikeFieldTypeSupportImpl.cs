using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCSharpUtils;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal sealed class VectorLikeFieldTypeSupportImpl : CSharpFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsVectorLike(type);

    // Vectors ignore a move within the epsilon, except when an axis changes from or to 0, the same as scalar floats.
    protected override void EmitEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        var zeroChecks = Axes(symbol).Select(axis => $"({context.State.CurrentVar}.{axis} == 0) == (value.{axis} == 0)");
        context.Append($"{string.Join(" && ", zeroChecks)} && {FullyQualifiedTypeName(context.State.CurrentType)}.DistanceSquared({context.State.CurrentVar}, value) <= {DeriveComponentUtils.VectorComparisonEpsilon}f");
    }

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        var zeroChecks = Axes(symbol).Select(axis => $"({context.State.CurrentVar}.{axis} == 0) != (value.{axis} == 0)");
        context.Append($"{string.Join(" || ", zeroChecks)} || {FullyQualifiedTypeName(context.State.CurrentType)}.DistanceSquared({context.State.CurrentVar}, value) > {DeriveComponentUtils.VectorComparisonEpsilon}f");
    }

    private static string[] Axes(ITypeSymbol symbol)
        => symbol.Name switch
        {
            "Vector2" => ["X", "Y"],
            "Vector3" => ["X", "Y", "Z"],
            "Vector4" => ["X", "Y", "Z", "W"],
            _ => throw new InvalidOperationException($"Unsupported vector type: {symbol.ToDisplayString()}"),
        };
}
