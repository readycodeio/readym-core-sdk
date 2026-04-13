namespace ReadyM.Api.Generators;

internal class DeriveTargetModel(
    DeriveTargetInfo sourceTarget,
    DeriveMaskInfo maskInfo,
    DeriveMemberModelWithSupport[] members)
{
    internal DeriveTargetInfo SourceTarget { get; } = sourceTarget;
    internal DeriveMaskInfo MaskInfo { get; } = maskInfo;
    internal DeriveMemberModelWithSupport[] Members { get; } = members;
}