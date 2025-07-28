using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

// FIXME: Move to ReadyM.Relay.Client
public class ClientSendComponentDeltaSystem<T>(NetworkedComponentId componentId, IRelayClient relay) : SendComponentDeltaSystemBase<T>(componentId)
    where T : struct, INetworkedComponent
{
    protected override int GetMaxPacketSize()
    {
        return relay.GetMaxPacketSize(DeliveryMethod.Unreliable);
    }

    protected override void Send(NetDataWriter data)
    {
        relay.OpRaiseEventRaw(data, DeliveryMethod.Unreliable);
    }

    protected override bool OwnsEntity(NetworkIdComponent netId)
    {
        return netId.Creator == relay.PlayerId;
    }
}