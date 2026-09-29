using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal class CSharpEmitSerializeContext(
    CSharpEmitState state,
    IDeriveSupportVisitor<ITypeSymbol, CSharpEmitSerializeContext> visitor,
    ICSharpSerializationCodec codec) : CSharpEmitContextBase(state)
{
    public readonly ICSharpSerializationCodec Codec = codec;

    public void EmitSerializeVar(string varName, ITypeSymbol varType)
    {
        using (State.WithCurrent(varName, varType))
        {
            visitor.Visit(varType, this);
        }
    }
}
