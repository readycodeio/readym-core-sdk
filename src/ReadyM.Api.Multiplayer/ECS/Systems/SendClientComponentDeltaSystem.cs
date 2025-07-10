using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Relay.Client;
using ReadyM.Relay.Common.ECS;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

public class SendClientComponentDeltaSystem<T>(NetworkedComponentId componentId, IRelayClient relay) : SendComponentDeltaSystemBase<T>(componentId)
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