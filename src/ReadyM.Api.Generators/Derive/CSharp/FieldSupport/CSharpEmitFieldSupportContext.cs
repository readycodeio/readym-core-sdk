using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Derive.CSharp.Serialization;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal class CSharpEmitFieldSupportContext(
    CSharpEmitState state,
    ITypeSymbol maskType,
    int maskIndex,
    IDeriveTypeSupportVisitor<CSharpEmitSerializeContext> serializeVisitor,
    IDeriveTypeSupportVisitor<CSharpEmitDeserializeContext> deserializeVisitor) : CSharpEmitContextBase(state)
{
    public readonly ITypeSymbol MaskType = maskType;
    public readonly int MaskIndex = maskIndex;

    public string CurrentMaskVar { get; private set; } = "_dirtyMask";

    public void EmitSerializeVar(string varName, ITypeSymbol varType)
    {
        using (State.WithCurrent(varName, varType))
        {
            var nestedContext = new CSharpEmitSerializeContext(State, serializeVisitor);
            serializeVisitor.Visit(varType, nestedContext);
        }
    }

    public void EmitDeserializeVar(string varName, ITypeSymbol varType)
    {
        using (State.WithCurrent(varName, varType))
        {
            var nestedContext = new CSharpEmitDeserializeContext(State, deserializeVisitor);
            deserializeVisitor.Visit(varType, nestedContext);
        }
    }

    public void SetCurrentMaskVarName(string maskVarName)
    {
        CurrentMaskVar = maskVarName;
    }
}