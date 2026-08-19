using System;

namespace ReadyM.Api.Generators.Derive.CSharp.ConflictResolution;

internal class CSharpEmitConflictSupportContext(
    CSharpEmitState state,
    DeriveMemberModel member,
    DeriveTargetModel model) : CSharpEmitContextBase(state)
{
    public readonly DeriveMemberModel Member = member;
    public readonly DeriveTargetModel Model = model;

    public string TypeName
        => Model.Source.Name;

    public string IdentVarName { get; private set; } = "id";

    private string? _changeStoreVar;
    private string? _lastObservedTimeVarName;

    public string ChangeStoreVar
        => _changeStoreVar ?? throw new InvalidOperationException("ChangeStoreVar has not been set");

    public string LastObservedTimeVar
        => _lastObservedTimeVarName ?? throw new InvalidOperationException("LastObservedTimeVar has not been set");

    public void SetIdent(string entity)
    {
        IdentVarName = entity;
    }

    public void SetResolver(string resolverVarName, string lastObservedTimeVar)
    {
        _changeStoreVar = resolverVarName;
        _lastObservedTimeVarName = lastObservedTimeVar;
    }
}
