using System.Collections.Generic;

namespace ReadyM.Api.Generators;

internal class DeriveTargetModel(
    DeriveTargetInfo source,
    IReadOnlyList<DeriveMemberModel> members,
    DeriveMaskInfo? maskInfo)
{
    public DeriveTargetInfo Source { get; } = source;
    public IReadOnlyList<DeriveMemberModel> Members { get; } = members;
    public DeriveMaskInfo? MaskInfo { get; } = maskInfo;
}