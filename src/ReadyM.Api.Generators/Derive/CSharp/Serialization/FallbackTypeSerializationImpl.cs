using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal class FallbackTypeSerializationImpl : CSharpTypeSerializationImplBase
{
    public override bool Supports(ITypeSymbol type)
        => true;

    protected override void EmitSerialize(ITypeSymbol symbol, CSharpEmitSerializeContext context)
        => context.AppendLine($"throw new NotSupportedException(\"Type '{symbol.ToDisplayString()}' is not supported for serialization.\");");

    protected override void EmitDeserialize(ITypeSymbol symbol, CSharpEmitDeserializeContext context)
        => context.AppendLine($"throw new NotSupportedException(\"Type '{symbol.ToDisplayString()}' is not supported for deserialization.\");");
}