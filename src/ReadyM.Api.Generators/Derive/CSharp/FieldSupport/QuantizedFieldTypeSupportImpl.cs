using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

/// A float or vector field marked [Quantize], [QuantizeAngle] or [HalfPrecision]. It counts as changed only when it
/// moves past the plain fields' cutoff and its encoding changes too, so it is sent no more often than a plain field and
/// a change too small for the wire neither marks it dirty nor drifts it from what was sent.
internal sealed class QuantizedFieldTypeSupportImpl : CSharpFieldTypeSupportImplBase
{
    public static readonly QuantizedFieldTypeSupportImpl Instance = new();

    public override bool Supports(ITypeSymbol type)
        => type.SpecialType == SpecialType.System_Single || SerializationHelper.IsVectorLike(type);

    protected override void EmitEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.Append(context.Member.Quantization!.UnchangedExpression(context.State.CurrentVar, "value"));

    protected override void EmitNotEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.Append("!" + context.Member.Quantization!.UnchangedExpression(context.State.CurrentVar, "value"));
}
