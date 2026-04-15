using System.Collections.Generic;

namespace ReadyM.Api.Generators;

internal class DeriveTargetModel(
    DeriveTargetInfo sourceTarget,
    DeriveMaskInfo maskInfo,
    IReadOnlyList<DeriveMemberModel> members)
{
    public DeriveTargetInfo SourceTarget { get; } = sourceTarget;
    public DeriveMaskInfo MaskInfo { get; } = maskInfo;
    public IReadOnlyList<DeriveMemberModel> Members { get; } = members;
}