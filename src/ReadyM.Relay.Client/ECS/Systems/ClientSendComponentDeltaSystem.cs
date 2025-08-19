using Friflo.Engine.ECS;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Relay.Common.ECS.Systems;

namespace ReadyM.Relay.Client.ECS.Systems;

public class ClientSendComponentDeltaSystem<T>(NetworkedComponentId componentId, IRelayClient relay) : SendComponentDeltaSystemBase<T>(componentId)
    where T : struct, INetworkedComponent
{
    protected override QueryFilter SetupFilter(QueryFilter filter, SendContext context)
        => filter;
    
    protected override int GetMaxPacketSize()
    {
        return relay.GetMaxPacketSize(DeliveryMethod.ReliableOrdered);
    }

    protected override void Send(NetDataWriter data, SendContext context)
    {
        relay.SendRawMessage(data, DeliveryMethod.ReliableOrdered);
    }

    protected override bool OwnsEntity(MetadataComponent meta, SendContext context)
    {
        return meta.Owner == relay.PlayerId; // TODO: use ClientOwnershipManager?
    }
}