using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Policies.Data;

namespace ReadyM.Relay.Client.Mapping.Policies;

internal class WorldAuthoritativeDataPolicy<TField>(DataSideChannel sideChannel) : MappingDataPolicyBase<TField, Entity>(sideChannel)
{
    protected override bool ShouldGameCopyToEcsImpl(in Entity context)
        => false;

    public override bool CanSetFromApi(in Entity context)
        => false;

    protected override bool ShouldEcsCopyToGameImpl(in Entity context)
        => true;

    protected override bool CanGameSetLocallyImpl(in Entity context)
        => false;
}
