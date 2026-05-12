using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadyM.Api.Generators;

internal sealed class DeriveTargetInfo(
    ITypeSymbol symbol,
    string name,
    string @namespace,
    IReadOnlyList<DeriveMemberInfo> members,
    bool isNullable,
    IReadOnlyList<string> errors,
    ITypeSymbol? requestedDirtyMaskType,
    bool emitDirtyMask,
    bool emitBindDelete,
    DeriveMapSettings mapSettings)
{
    public ITypeSymbol Symbol { get; } = symbol ?? throw new ArgumentNullException(nameof(symbol));
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    public string Namespace { get; } = @namespace ?? throw new ArgumentNullException(nameof(@namespace));

    private readonly List<DeriveMemberInfo> _members = members != null ? [..members] : throw new ArgumentNullException(nameof(members));
    public IReadOnlyList<DeriveMemberInfo> Members => _members;
    
    public bool IsNullable { get; } = isNullable;
    public ITypeSymbol? RequestedDirtyMaskType { get; } = requestedDirtyMaskType;
    public bool EmitDirtyMask { get; } = emitDirtyMask;
    public bool EmitBindDelete { get; } = emitBindDelete;
    public DeriveMapSettings MapSettings { get; } = mapSettings;

    private readonly List<string> _errors = [..errors ?? throw new ArgumentNullException(nameof(errors))];
    
    public bool HasErrors
        => _errors.Count > 0;

    public IReadOnlyList<string> Errors => _errors;
    
    public void AddError(string error)
    {
        _errors.Add(error);
    }
    
    public void AddMember(DeriveMemberInfo memberInfo)
    {
        _members.Add(memberInfo);
    }
}