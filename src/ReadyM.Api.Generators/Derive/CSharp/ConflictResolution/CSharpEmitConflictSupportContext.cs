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

    public string EntityVarName { get; private set; } = "entity";

    private string? _resolveConflictsVarName;
    private string? _lastObservedTimeVarName;

    public string ResolveConflictsVar
        => _resolveConflictsVarName ?? throw new InvalidOperationException("ResolverVarName has not been set");

    public string LastObservedTimeVar
        => _lastObservedTimeVarName ?? throw new InvalidOperationException("ObservedTimeVarName has not been set");

    public void SetEntity(string entity)
    {
        EntityVarName = entity;
    }

    public void SetResolver(string resolveVarName, string lastObservedTimeVar)
    {
        _resolveConflictsVarName = resolveVarName;
        _lastObservedTimeVarName = lastObservedTimeVar;
    }
}
