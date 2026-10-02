using ReadyM.Api.Helpers;

namespace ReadyM.Api.Mapping.Policies.Data.Common;

internal class ServerAuthoritativeDataPolicy<TField, TContext>(DataSideChannel sideChannel)
    : MappingDataPolicyBase<TField, TContext>(sideChannel)
{
    protected override bool ShouldGameCopyToEcsImpl(in TContext context) => false;

    protected override bool ShouldEcsCopyToGameImpl(in TContext context) => true;

    public override bool CanSetFromApi(in TContext context) => false;

    protected override bool CanGameSetLocallyImpl(in TContext context) => false;
}
