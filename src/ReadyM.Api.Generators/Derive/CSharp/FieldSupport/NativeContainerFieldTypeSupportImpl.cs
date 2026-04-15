using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal class NativeContainerFieldTypeSupportImpl : CSharpFieldTypeSupportImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsNativeContainer(type);

    protected override void EmitEqualCheck(ITypeSymbol symbol, CSharpEmitFieldSupportContext context)
        => context.Append($"{context.State.CurrentVar}.Equals(value)");
}