using LiteNetLib.Utils;
using ReadyM.Api.DI;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Relay.Server.Sdk.Rpc;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Events;

namespace ReadyM.SDK.Server.Events;

/// <summary>
/// Receives one event, from a client or from server code, checks its policy, runs every mod's handlers, and sends
/// it to its audience or the refusal back to its origin.
/// </summary>
internal sealed class ServerNetworkedEventCoordinator<T>(
    INetworkedEventContract<T> contract,
    IEnumerable<INetworkedEventHandlers<T>> handlers,
    RpcApi relay,
    INetworkedEventEntityReader entities,
    ServerNetworkedEventPolicy policy,
    ServerNetworkedEventScopes scopes,
    ServerNetworkedEventSender sender)
    : NetworkedEventCoordinatorBase<T>(handlers), IHostedService
    where T : struct, INetworkedEvent
{
    /// <summary>What <see cref="EventContext.CancelledBy"/> says when the policy refused the event.</summary>
    private const string PolicyCancellerName = "policy";

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

    /// <summary>Raises the event from server code as <paramref name="origin"/>. The policy is not checked.</summary>
    public void Raise(T networkEvent, PlayerId origin) => Dispatch(networkEvent, origin, checkPolicy: false);

    private void OnMessageReceived(ServerEventHeader header, NetDataReader reader)
    {
        if (contract.TryRead(entities, reader, out var networkEvent))
            Dispatch(networkEvent, header.Sender, checkPolicy: true);
    }

    internal void Dispatch(T networkEvent, PlayerId origin, bool checkPolicy)
    {
        if (checkPolicy && !policy.Allows(
                contract.Policy,
                contract.Subject(in networkEvent),
                contract.TryArea(in networkEvent, out var area) ? area : null,
                contract.TryCell(in networkEvent, out var cell) ? cell : null,
                origin))
        {
            Refuse(ref networkEvent, new EventContext(origin, false, EventCancellation.Cancelled(PolicyCancellerName, null)));
            return;
        }

        var context = new EventContext(origin, false, new EventCancellation());
        RunStage(Stage.Validate, ref networkEvent, context);
        RunStage(Stage.Filter, ref networkEvent, context);

        if (context.IsCancelled)
        {
            Refuse(ref networkEvent, context);
            return;
        }

        RunStage(Stage.Apply, ref networkEvent, context);
        RunStage(Stage.Observe, ref networkEvent, context);

        if (TryResolveAudience(in networkEvent, out var audience))
            sender.SendAccepted(contract.Code, audience, origin, Serialize(in networkEvent));
    }

    private void Refuse(ref T networkEvent, EventContext context)
    {
        var payload = Serialize(in networkEvent);
        payload.Put(context.CancelledBy);
        contract.WriteReason(payload, context.CancellationReason);

        sender.SendRefusal(contract.Code, context.Origin, payload);
        RunStage(Stage.Rejected, ref networkEvent, context);
    }

    /// <summary>
    /// Who hears an accepted event. False for a server-scoped event, and for an event in an area or cell that
    /// neither names one nor has a subject in one.
    /// </summary>
    private bool TryResolveAudience(in T networkEvent, out EventAudience audience)
    {
        audience = default;

        switch (contract.Scope)
        {
            case EventScope.Server:
                return false;

            case EventScope.Global:
                audience = EventAudience.Global;
                return true;

            case EventScope.Single:
                audience = EventAudience.Single(contract.Target(in networkEvent));
                return true;

            case EventScope.Area:
                if (!contract.TryArea(in networkEvent, out var area)
                    && !(contract.Subject(in networkEvent) is { } inArea && scopes.TryFindAreaOf(inArea, out area)))
                    return false;

                audience = EventAudience.InArea(area);
                return true;

            case EventScope.Cell:
                if (!contract.TryCell(in networkEvent, out var cell)
                    && !(contract.Subject(in networkEvent) is { } inCell && scopes.TryFindCellOf(inCell, out cell)))
                    return false;

                audience = EventAudience.InCell(cell);
                return true;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(networkEvent), contract.Scope, "Unhandled networked event scope.");
        }
    }

    private static NetDataWriter Serialize(in T networkEvent)
    {
        var writer = new NetDataWriter();
        networkEvent.Serialize(writer);

        return writer;
    }
}
