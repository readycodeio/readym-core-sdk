using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal sealed class EquatableFieldTypeSupportImpl : CSharpFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsEquatable(type);

    protected override void EmitEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
    {
        if (symbol.IsValueType)
            context.Append($"{context.State.CurrentVar}.Equals(value)");
        else
            context.Append($"{context.State.CurrentVar}?.Equals(value) ?? value is null");
    }
}