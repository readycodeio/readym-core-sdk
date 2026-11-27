using ReadyM.Api.Idents;

namespace ReadyM.SDK.Events;

/// <summary>Allows raising networked events.</summary>
public interface INetworkedEvents
{
    /// <summary>Raises the event. The origin is this client, or the server.</summary>
    /// <returns>A handle a client may use to provide prediction and rollback logic while it waits for the answer.</returns>
    INetworkedEventRaise Raise<T>(T networkEvent) where T : struct, INetworkedEvent;

    /// <summary>
    /// Server only: raises it as though <paramref name="origin"/> did, so handlers cannot tell it from that
    /// player's own event. The policy check is skipped, because the server vouched for it.
    /// </summary>
    /// <remarks>
    /// Useful for implementing server-side bots or AI that act on behalf of a player,
    /// or for a server-side mod that wants to raise an event as though it came from a player.
    /// </remarks>
    void RaiseAs<T>(PlayerId origin, T networkEvent) where T : struct, INetworkedEvent;

    /// <summary>
    /// Server only: raises it from the server in <paramref name="dimension"/>, for an event with no subject, which
    /// <see cref="Raise{T}"/> puts in the main dimension. An area or cell the event names by id is that dimension's.
    /// </summary>
    /// <exception cref="System.ArgumentException">The event's subject is in another dimension.</exception>
    void RaiseIn<T>(DimensionId dimension, T networkEvent) where T : struct, INetworkedEvent;
}
