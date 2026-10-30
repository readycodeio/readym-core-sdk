using System.Collections.Generic;
using Friflo.Engine.ECS;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ConflictResolution;
using ReadyM.Api.Multiplayer.ECS;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Systems;

namespace ReadyM.Relay.Client.ECS.Systems;

internal class ClientSendComponentDeltaSystem<T>(
    NetworkedComponentId componentId,
    INetworkTime networkTime,
    DeliveryMethod deliveryMethod, IRelayClient relay,
    PredictionDeltaHooks<T> predictionHooks)
	: SendComponentDeltaSystemBase<T>(networkTime, componentId, false, predictionHooks)
    where T : struct, INetworkedComponent
{
    protected override QueryFilter SetupFilter(QueryFilter filter, SendContext context)
        => filter;

    protected override int? GetMaxPacketSize()
        => deliveryMethod == DeliveryMethod.ReliableOrdered ? null : relay.GetMaxPacketSize(deliveryMethod);

    protected override void SentOwners(HashSet<PlayerId> owners)
    {
        // Read once. PlayerId is set on the network thread and cleared on disconnect, and this runs on the
        // ECS thread, so checking it and then using it may read two different values.
        if (relay.PlayerId is {} id)
        {
            owners.Add(id);
        }
    }

    protected override void SendExceptOwner(PlayerId _, NetDataWriter data, SendContext context)
    {
        relay.SendRawMessage(data, deliveryMethod);
    }
}
