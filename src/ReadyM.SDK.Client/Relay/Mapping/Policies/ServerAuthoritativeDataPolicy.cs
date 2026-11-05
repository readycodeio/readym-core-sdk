using Friflo.Engine.ECS;
using ReadyM.Api.Helpers;
using ReadyM.Api.Mapping.Policies.Data;
using ReadyM.SDK.Client.Relay.State;

namespace ReadyM.SDK.Client.Relay.Mapping.Policies;

/// <summary>The server sets the data, and it reaches this game only for an entity this client owns.</summary>
internal class ServerAuthoritativeDataPolicy<TField>(ClientOwnershipManager ownership, DataSideChannel sideChannel)
    : MappingDataPolicyBase<TField, Entity>(sideChannel)
{
    protected override bool ShouldGameCopyToEcsImpl(in Entity context)
        => false;

    public override bool CanSetFromApi(in Entity context)
        => ownership.OwnsEntity(context);

    protected override bool ShouldEcsCopyToGameImpl(in Entity context)
        => ownership.OwnsEntity(context);

    protected override bool CanGameSetLocallyImpl(in Entity context)
        => false;
}
