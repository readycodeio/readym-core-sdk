using LiteNetLib;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Extensions;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Relay.Server.Sdk.Players;
using ReadyM.Relay.Server.Sdk.Rpc;
using ReadyM.SDK.Archetypes.Core;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Events;
using ReadyM.SDK.Server.Entities;

namespace ReadyM.SDK.Server.Events;

/// <summary>Sends accepted events to their audience, and refusals back to their origin.</summary>
internal sealed class ServerNetworkedEventSender(RpcApi relay, IEntities entities, IPlayers players)
{
    public void SendAccepted(RelayMessageCode code, EventAudience audience, PlayerId origin, NetDataWriter payload)
    {
        var message = Message(code, new NetworkedEventHeader(false, origin), payload);

        foreach (var player in PlayersIn(audience))
            relay.SendToOne(player, message, DeliveryMethod.ReliableOrdered);
    }

    public void SendRefusal(RelayMessageCode code, PlayerId origin, NetDataWriter payload)
    {
        // An event the server raised has nobody waiting for the answer
        if (origin == PlayerId.Server)
            return;

        relay.SendToOne(origin, Message(code, new NetworkedEventHeader(true, origin), payload),
            DeliveryMethod.ReliableOrdered);
    }

    private static NetDataWriter Message(RelayMessageCode code, NetworkedEventHeader header, NetDataWriter payload)
    {
        var message = new NetDataWriter();
        message.PutMessageCode(code);
        header.Write(message);
        message.Put(payload.Data, 0, payload.Length);

        return message;
    }

    private List<PlayerId> PlayersIn(EventAudience audience)
    {
        switch (audience.Kind)
        {
            case EventScope.Single:
                return [audience.Player];

            case EventScope.Global:
                var everyone = new List<PlayerId>();
                foreach (var player in entities.Query<Player>())
                    everyone.Add(player.PlayerId);
                return everyone;

            case EventScope.Area:
                return players.InArea(audience.Area);

            case EventScope.Cell:
                return players.InCell(audience.Cell);

            case EventScope.Server:
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(audience), audience.Kind, "Unhandled networked event audience.");
        }
    }
}
