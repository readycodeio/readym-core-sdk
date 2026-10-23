using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using static ReadyM.Api.Generators.DeriveCppUtils;

namespace ReadyM.Api.Generators.Derive.Cpp.FieldSupport;

internal sealed class VectorLikeFieldTypeSupportImpl : CppNonOverrideFieldTypeSupportImplBase
{
    protected override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsVectorLike(type);

    // Vectors ignore a move within the epsilon, except when an axis changes from or to 0, the same as scalar floats.
    protected override void EmitEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        var cppTypeName = CppTypeName(context.State.CurrentType);
        var zeroChecks = Axes(context.State.CurrentType).Select(axis => $"({context.State.CurrentVar}.{axis} == 0) == (value.{axis} == 0)");

        context.Append($"{string.Join(" && ", zeroChecks)} && {cppTypeName}::DistanceSquared({context.State.CurrentVar}, value) <= {DeriveComponentUtils.VectorComparisonEpsilon}f");
    }

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CppEmitFieldSupportContext context)
    {
        var cppTypeName = CppTypeName(context.State.CurrentType);
        var zeroChecks = Axes(context.State.CurrentType).Select(axis => $"({context.State.CurrentVar}.{axis} == 0) != (value.{axis} == 0)");

        context.Append($"{string.Join(" || ", zeroChecks)} || {cppTypeName}::DistanceSquared({context.State.CurrentVar}, value) > {DeriveComponentUtils.VectorComparisonEpsilon}f");
    }

    private static string[] Axes(ITypeSymbol type)
        => type.Name switch
        {
            "Vector2" => ["X", "Y"],
            "Vector3" => ["X", "Y", "Z"],
            "Vector4" => ["X", "Y", "Z", "W"],
            _ => throw new InvalidOperationException($"Unsupported vector type: {type.ToDisplayString()}"),
        };
}
