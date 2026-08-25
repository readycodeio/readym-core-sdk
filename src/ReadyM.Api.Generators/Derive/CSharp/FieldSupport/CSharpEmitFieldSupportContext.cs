using System;
using Microsoft.CodeAnalysis;
using ReadyM.Api.Generators.Derive.CSharp.ConflictResolution;
using ReadyM.Api.Generators.Derive.CSharp.Serialization;

namespace ReadyM.Api.Generators.Derive.CSharp.FieldSupport;

internal class CSharpEmitFieldSupportContext(
    CSharpEmitState state,
    DeriveMemberModel member,
    DeriveTargetModel model,
    IDeriveSupportVisitor<ITypeSymbol, CSharpEmitSerializeContext> serializeVisitor,
    IDeriveSupportVisitor<ITypeSymbol, CSharpEmitDeserializeContext> deserializeVisitor) : CSharpEmitContextBase(state)
{
    public readonly DeriveMemberModel Member = member;
    public readonly DeriveTargetModel Model = model;

    public string TypeName
        => Model.Source.Name;

    public string CurrentMaskVar { get; private set; } = "_dirtyMask";
    public string CurrentApiMaskVar { get; private set; } = "_apiMask";

    private string? _autoMarkApiOnWriteVar;

    public string AutoMarkApiOnWriteVar
        => _autoMarkApiOnWriteVar ?? throw new InvalidOperationException("AutoMarkApiOnWrite not set");

    private ICSharpEmitConflictSupportImpl? _emitConflict;
    private CSharpEmitConflictSupportContext? _emitConflictContext;

    public ICSharpEmitConflictSupportImpl EmitConflict
        => _emitConflict ?? throw new InvalidOperationException("ConflictResolver not set");

    public CSharpEmitConflictSupportContext EmitConflictContext
        => _emitConflictContext ?? throw new InvalidOperationException("ConflictResolver not set");

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

    public void SetCurrentMaskVarName(string varName)
    {
        CurrentMaskVar = varName;
    }

    public void SetAutoMark(string autoMarkApiOnWriteVar)
    {
        _autoMarkApiOnWriteVar = autoMarkApiOnWriteVar;
    }

    public void SetEmitConflictResolver(ICSharpEmitConflictSupportImpl emitConflict, CSharpEmitConflictSupportContext context)
    {
        _emitConflict = emitConflict;
        _emitConflictContext = context;
    }
}
