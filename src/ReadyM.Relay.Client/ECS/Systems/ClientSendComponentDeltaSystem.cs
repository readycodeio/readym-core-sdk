using Friflo.Engine.ECS;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ConflictResolution;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Systems;

namespace ReadyM.Relay.Client.ECS.Systems;

internal class ClientSendComponentDeltaSystem<T>(
    NetworkedComponentId componentId,
    INetworkTime networkTime,
    DeliveryMethod deliveryMethod, IRelayClient relay)
	: SendComponentDeltaSystemBase<T>(networkTime, componentId, false)
    where T : struct, INetworkedComponent
{
    protected override QueryFilter SetupFilter(QueryFilter filter, SendContext context)
        => filter;

    protected override int? GetMaxPacketSize()
        => deliveryMethod == DeliveryMethod.ReliableOrdered ? null : relay.GetMaxPacketSize(deliveryMethod);

    protected override uint SentOwners()
    {
        // Read once. PlayerId is set on the network thread and cleared on disconnect, and this runs on the
        // ECS thread, so checking it and then using it read two different values: during teardown the check
        // passed and the use threw "Nullable object must have a value".
        var playerId = relay.PlayerId;
        return playerId.HasValue ? 1u << playerId.Value.RawValue : 0;
    }

    protected override void SendExceptOwner(PlayerId _, NetDataWriter data, SendContext context)
    {
        relay.SendRawMessage(data, deliveryMethod);
    }
}
