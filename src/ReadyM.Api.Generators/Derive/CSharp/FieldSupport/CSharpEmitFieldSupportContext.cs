using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Derive.CSharp.Serialization;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal class CSharpEmitFieldSupportContext(
    CSharpEmitState state,
    DeriveMemberModel member,
    DeriveTargetModel model,
    IDeriveTypeSupportVisitor<CSharpEmitSerializeContext> serializeVisitor,
    IDeriveTypeSupportVisitor<CSharpEmitDeserializeContext> deserializeVisitor) : CSharpEmitContextBase(state)
{
    public readonly DeriveMemberModel Member = member;
    public readonly DeriveTargetModel Model = model;

    public string CurrentMaskVar { get; private set; } = "_dirtyMask";
    public string CurrentApiMaskVar { get; private set; } = "_apiMask";

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