using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal sealed class PrimitiveSerializationImpl : CSharpTypeSerializationImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsSerializablePrimitive(type.SpecialType);

    protected override void EmitSerialize(ITypeSymbol symbol, CSharpEmitSerializeContext context)
        => context.Codec.WritePrimitive(context);

    protected override void EmitDeserialize(ITypeSymbol symbol, CSharpEmitDeserializeContext context)
        => context.Codec.ReadPrimitive(context);
}
