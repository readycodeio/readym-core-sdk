using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal abstract class CSharpTypeSerializationImplBase : ICSharpTypeSerializationImpl
{
    public abstract bool Supports(ITypeSymbol type);

    public void Visit(ITypeSymbol symbol, CSharpEmitSerializeContext context)
        => EmitSerialize(symbol, context);

    public void Visit(ITypeSymbol symbol, CSharpEmitDeserializeContext context)
        => EmitDeserialize(symbol, context);

    protected abstract void EmitSerialize(ITypeSymbol symbol, CSharpEmitSerializeContext context);
    protected abstract void EmitDeserialize(ITypeSymbol symbol, CSharpEmitDeserializeContext context);
}