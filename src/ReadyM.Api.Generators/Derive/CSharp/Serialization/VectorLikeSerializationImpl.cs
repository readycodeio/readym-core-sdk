using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal sealed class VectorLikeSerializationImpl : CSharpTypeSerializationImplBase
{
    public override bool Supports(ITypeSymbol type)
        => SerializationHelper.IsVectorLike(type);

    protected override void EmitSerialize(ITypeSymbol symbol, CSharpEmitSerializeContext context)
        => context.AppendLine($"{context.State.CurrentVar}.Serialize(writer);");

    protected override void EmitDeserialize(ITypeSymbol symbol, CSharpEmitDeserializeContext context)
        => context.AppendLine($"{context.State.CurrentVar}.Deserialize(reader);");
}