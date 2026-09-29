using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal class CSharpEmitDeserializeContext(
    CSharpEmitState state,
    IDeriveSupportVisitor<ITypeSymbol, CSharpEmitDeserializeContext> visitor,
    ICSharpSerializationCodec codec) : CSharpEmitContextBase(state)
{
    public readonly ICSharpSerializationCodec Codec = codec;

    public void EmitDeserializeVar(string varName, ITypeSymbol varType)
    {
        using (State.WithCurrent(varName, varType))
        {
            visitor.Visit(varType, this);
        }
    }
}
