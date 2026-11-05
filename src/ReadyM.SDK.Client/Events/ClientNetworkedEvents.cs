using LiteNetLib;
using ReadyM.Api.ECS.Worlds;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.SDK.Events;

namespace ReadyM.SDK.Client.Events;

/// <summary>Sends raised events to the server, which runs their stages and answers.</summary>
internal sealed class ClientNetworkedEvents(IRpcClient relay, PredictedRaiseRegistry predictedRaises, Store world)
    : INetworkedEvents
{
    /// <summary>Set from the client's update, so a prediction can time out while nothing answers it.</summary>
    public double Now { get; set; }

    public INetworkedEventRaise Raise<T>(T networkEvent) where T : struct, INetworkedEvent
    {
        if (!world.IsOnOwnThread)
            throw new EventRaisedOffEcsThreadException(typeof(T));

        var message = RelayMessage.ToServer(networkEvent.Code, DeliveryMethod.ReliableOrdered);
        networkEvent.Serialize(message.Writer);
        relay.SendMessage(message);

        return predictedRaises.StartRaise(networkEvent.Code, Now);
    }

    public void RaiseAs<T>(PlayerId origin, T networkEvent) where T : struct, INetworkedEvent
        => throw new InvalidOperationException(
            $"Only the server may raise an event as another player. {typeof(T).Name} was raised as {origin} on a client.");
}
