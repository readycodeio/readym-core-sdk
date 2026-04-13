using System;

namespace ReadyM.Api.Generators;

internal sealed class DeriveTargetInfo(
    string name,
    string @namespace,
    DeriveMemberInfo[] members,
    bool isNullable,
    string[] errorMessages,
    string? dirtyMaskType,
    bool emitDirtyMask,
    DeriveMapSettings mapSettings)
{
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    public string Namespace { get; } = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
    public DeriveMemberInfo[] Members { get; } = members ?? throw new ArgumentNullException(nameof(members));
    public bool IsNullable { get; } = isNullable;
    public string[] ErrorMessages { get; } = errorMessages ?? throw new ArgumentNullException(nameof(errorMessages));
    public string? DirtyMaskType { get; } = dirtyMaskType;
    public bool EmitDirtyMask { get; } = emitDirtyMask;
    public DeriveMapSettings MapSettings { get; } = mapSettings;
}