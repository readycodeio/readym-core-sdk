using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal sealed class EntitySerializationImpl : CSharpTypeSerializationImplBase
{
    public override bool Supports(ITypeSymbol type)
        => type.Name == "Entity" && type.ContainingNamespace?.ToDisplayString() == "Friflo.Engine.ECS";

    protected override void EmitSerialize(ITypeSymbol symbol, CSharpEmitSerializeContext context)
        => context.Codec.WriteEntityRef(context);

    protected override void EmitDeserialize(ITypeSymbol symbol, CSharpEmitDeserializeContext context)
        => context.Codec.ReadEntityRef(context);
}
