using Friflo.Engine.ECS;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

public class ClientSendComponentDeltaSystem<T>(NetworkedComponentId componentId, IRelayClient relay) : SendComponentDeltaSystemBase<T>(componentId)
    where T : struct, INetworkedComponent
{
    protected override ArchetypeQuery<MetadataComponent, T> GetQuery(EmptyContext context)
        => Query;
    
    protected override int GetMaxPacketSize()
    {
        return relay.GetMaxPacketSize(DeliveryMethod.ReliableOrdered);
    }

    protected override void Send(NetDataWriter data, EmptyContext context)
    {
        relay.SendRawMessage(data, DeliveryMethod.ReliableOrdered);
    }

    protected override bool OwnsEntity(MetadataComponent meta, EmptyContext context)
    {
        return meta.Owner == relay.PlayerId;
    }
}