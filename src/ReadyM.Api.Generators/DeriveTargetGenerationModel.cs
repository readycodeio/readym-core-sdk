using System;

namespace ReadyM.Api.Generators;

internal sealed class DeriveTargetGenerationModel(
    DeriveTargetInfo targetInfo,
    DeriveMaskInfo mask,
    DeriveMemberGenerationModel[] members,
    bool emitDirtyMask)
{
    public DeriveTargetInfo TargetInfo { get; } = targetInfo ?? throw new ArgumentNullException(nameof(targetInfo));
    public DeriveMaskInfo Mask { get; } = mask ?? throw new ArgumentNullException(nameof(mask));
    public DeriveMemberGenerationModel[] Members { get; } = members ?? throw new ArgumentNullException(nameof(members));
    public bool EmitDirtyMask { get; } = emitDirtyMask;
}