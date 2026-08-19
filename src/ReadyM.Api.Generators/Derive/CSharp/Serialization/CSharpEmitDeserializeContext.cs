using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal class CSharpEmitDeserializeContext(
    CSharpEmitState state,
    IDeriveSupportVisitor<ITypeSymbol, CSharpEmitDeserializeContext> visitor) : CSharpEmitContextBase(state)
{
    public void EmitDeserializeVar(string varName, ITypeSymbol varType)
    {
        using (State.WithCurrent(varName, varType))
        {
            visitor.Visit(varType, this);
        }
    }
}
