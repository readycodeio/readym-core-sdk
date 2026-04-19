using System.Collections.Generic;

namespace ReadyM.Api.Generators;

internal class DeriveTargetModel(
    DeriveTargetInfo sourceTarget,
    IReadOnlyList<DeriveMemberModel> members,
    DeriveMaskInfo? maskInfo)
{
    public DeriveTargetInfo SourceTarget { get; } = sourceTarget;
    public IReadOnlyList<DeriveMemberModel> Members { get; } = members;
    public DeriveMaskInfo? MaskInfo { get; } = maskInfo;
}