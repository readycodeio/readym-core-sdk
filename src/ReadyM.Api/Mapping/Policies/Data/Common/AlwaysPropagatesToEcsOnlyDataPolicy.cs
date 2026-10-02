using ReadyM.Api.Helpers;

namespace ReadyM.Api.Mapping.Policies.Data.Common;

internal class AlwaysPropagatesToEcsOnlyDataPolicy<TField, TContext>(DataSideChannel sideChannel)
    : MappingDataPolicyBase<TField, TContext>(sideChannel)
{
    protected override bool ShouldGameCopyToEcsImpl(in TContext context) => true;

    protected override bool ShouldEcsCopyToGameImpl(in TContext context) => false;

    public override bool CanSetFromApi(in TContext context) => false;

    protected override bool CanGameSetLocallyImpl(in TContext context) => true;
}
