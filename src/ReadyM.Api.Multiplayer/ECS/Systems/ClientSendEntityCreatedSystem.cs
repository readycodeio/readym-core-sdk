using Friflo.Engine.ECS;
using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Systems;

public class ClientSendEntityCreatedSystem(IRelayClient relay) : SendEntityCreatedSystemBase
{
    protected override ArchetypeQuery<MetadataComponent> GetQuery(EmptyContext context)
        => Query;

    protected override void Send(NetDataWriter data, EmptyContext context)
    {
        relay.SendRawMessage(data, DeliveryMethod.ReliableOrdered);
    }
}