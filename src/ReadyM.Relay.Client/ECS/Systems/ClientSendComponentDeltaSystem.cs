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
        return relay.GetMaxPacketSize(DeliveryMethod.Unreliable);
    }

    protected override uint SentOwners()
    {
        if (relay.PlayerId.HasValue)
        {
            return 1u << relay.PlayerId.Value.RawValue;
        }

        return 0;
    }

    protected override void Send(PlayerId _, NetDataWriter data, SendContext context)
    {
        relay.SendRawMessage(data, DeliveryMethod.Unreliable);
    }
}