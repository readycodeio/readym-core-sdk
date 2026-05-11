using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators.Derive.CSharp.Serialization;

internal class CSharpEmitSerializeContext(
    CSharpEmitState state,
    IDeriveTypeSupportVisitor<CSharpEmitSerializeContext> visitor) : CSharpEmitContextBase(state)
{
    public void EmitSerializeVar(string varName, ITypeSymbol varType)
    {
        using (State.WithCurrent(varName, varType))
        {
            visitor.Visit(varType, this);
        }
    }
}