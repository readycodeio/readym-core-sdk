using LiteNetLib.Utils;
using ReadyM.Api.DI;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Client;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Events;

/// <summary>Receives one event from the server and runs every mod's Apply and Observe, or their Rejected.</summary>
internal sealed class ClientNetworkedEventCoordinator<T>(
    INetworkedEventContract<T> contract,
    IEnumerable<INetworkedEventHandlers<T>> handlers,
    IRpcClient relay,
    INetworkedEventEntityReader entities,
    PredictedRaiseRegistry predictedRaises)
    : NetworkedEventCoordinatorBase<T>(handlers), IHostedService
    where T : struct, INetworkedEvent
{
    // Kept, because the relay removes a handler by delegate identity
    private Action<ServerEventHeader, NetDataReader>? _onMessageReceived;

    public void OnScopeStart()
    {
        _onMessageReceived = OnMessageReceived;
        relay.AddServerRpcMessageHandler(contract.Code, _onMessageReceived);
    }

    public void Dispose()
    {
        if (_onMessageReceived is { } handler)
            relay.RemoveServerRpcMessageHandler(contract.Code, handler);

        _onMessageReceived = null;
    }

    private void OnMessageReceived(ServerEventHeader header, NetDataReader reader)
    {
        if (!NetworkedEventHeader.TryRead(reader, out var eventHeader)
            || !contract.TryRead(entities, reader, out var networkEvent))
            return;

        if (eventHeader.Refused)
        {
            OnRefusalReceived(ref networkEvent, reader, eventHeader.Origin);
            return;
        }

        var context = new EventContext(eventHeader.Origin, eventHeader.Origin == relay.PlayerId, new EventCancellation());
        RunStage(Stage.Apply, ref networkEvent, context);
        RunStage(Stage.Observe, ref networkEvent, context);
    }

    private void OnRefusalReceived(ref T networkEvent, NetDataReader reader, PlayerId origin)
    {
        predictedRaises.RollBackRefused(contract.Code);

        var cancellation = EventCancellation.Cancelled(reader.GetString(), contract.ReadReason(reader));
        RunStage(Stage.Rejected, ref networkEvent, new EventContext(origin, origin == relay.PlayerId, cancellation));
    }
}
