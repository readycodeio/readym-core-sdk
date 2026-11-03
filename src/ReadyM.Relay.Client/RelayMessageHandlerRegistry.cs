using System;
using System.Collections.Concurrent;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Protocol;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Relay.Client;

/// <summary>
/// A client's handlers for the messages it receives, by code. Handlers are added on the game thread and invoked on
/// the network thread, so each code's handler is replaced whole rather than changed in place.
/// </summary>
internal sealed class RelayMessageHandlerRegistry
{
    private readonly ConcurrentDictionary<RelayMessageCode, Action<ServerEventHeader, NetDataReader>> _serverHandlers = new();

    private readonly ConcurrentDictionary<RelayMessageCode, Action<CustomRelayEventHeader, NetDataReader>> _clientHandlers = new();

    /// <summary>Invoked for every built-in message, beside its own handlers.</summary>
    public Action<ServerEventHeader, NetDataReader>? AnyBuiltIn;

    /// <summary>Invoked for every server RPC and networked event message, beside its own handlers.</summary>
    public Action<ServerEventHeader, NetDataReader>? AnyServerRpc;

    /// <summary>Invoked for every message relayed from another client, beside its own handlers.</summary>
    public Action<CustomRelayEventHeader, NetDataReader>? AnyClientRpc;

    public void AddBuiltIn(RelayMessageCode min, RelayMessageCode max, Action<ServerEventHeader, NetDataReader> handler)
    {
        RequireBuiltIn(min, max);
        Add(_serverHandlers, min, max, handler);
    }

    public void RemoveBuiltIn(RelayMessageCode min, RelayMessageCode max, Action<ServerEventHeader, NetDataReader> handler)
    {
        RequireBuiltIn(min, max);
        Remove(_serverHandlers, min, max, handler);
    }

    public void AddServerRpc(RelayMessageCode min, RelayMessageCode max, Action<ServerEventHeader, NetDataReader> handler)
    {
        RequireCustom(min, max);
        Add(_serverHandlers, min, max, handler);
    }

    public void RemoveServerRpc(RelayMessageCode min, RelayMessageCode max, Action<ServerEventHeader, NetDataReader> handler)
    {
        RequireCustom(min, max);
        Remove(_serverHandlers, min, max, handler);
    }

    public void AddClientRpc(RelayMessageCode min, RelayMessageCode max, Action<CustomRelayEventHeader, NetDataReader> handler)
    {
        RequireCustom(min, max);
        Add(_clientHandlers, min, max, handler);
    }

    public void RemoveClientRpc(RelayMessageCode min, RelayMessageCode max, Action<CustomRelayEventHeader, NetDataReader> handler)
    {
        RequireCustom(min, max);
        Remove(_clientHandlers, min, max, handler);
    }

    /// <summary>Invokes the handlers for a message the server sent.</summary>
    /// <returns>False when nothing handles it.</returns>
    public bool InvokeServerMessage(ServerEventHeader header, NetDataReader reader)
    {
        var any = header.EventCode <= RelayMessageCode.MaxBuiltInEvent ? AnyBuiltIn : AnyServerRpc;
        _serverHandlers.TryGetValue(header.EventCode, out var handlers);

        return Invoke(handlers, header, reader) | Invoke(any, header, reader);
    }

    /// <summary>Invokes the handlers for a message relayed from another client.</summary>
    /// <returns>False when nothing handles it.</returns>
    public bool InvokeClientMessage(CustomRelayEventHeader header, NetDataReader reader)
    {
        _clientHandlers.TryGetValue(header.EventCode, out var handlers);

        return Invoke(handlers, header, reader) | Invoke(AnyClientRpc, header, reader);
    }

    /// <summary>Each handler reads the message from where it starts.</summary>
    private static bool Invoke<THeader>(Action<THeader, NetDataReader>? handlers, THeader header, NetDataReader reader)
    {
        if (handlers is null)
            return false;

        var position = reader.Position;

        foreach (var handler in handlers.GetInvocationList())
        {
            reader.SetPosition(position);
            ((Action<THeader, NetDataReader>)handler).Invoke(header, reader);
        }

        return true;
    }

    private static void Add<THandler>(
        ConcurrentDictionary<RelayMessageCode, THandler> handlers, RelayMessageCode min, RelayMessageCode max, THandler handler)
        where THandler : Delegate
    {
        // Counted as int: the last code is ushort.MaxValue, and a ushort counter would wrap past it forever
        for (var code = (int)min; code <= (int)max; code++)
            handlers.AddOrUpdate((RelayMessageCode)code, handler, (_, existing) => (THandler)Delegate.Combine(existing, handler));
    }

    private static void Remove<THandler>(
        ConcurrentDictionary<RelayMessageCode, THandler> handlers, RelayMessageCode min, RelayMessageCode max, THandler handler)
        where THandler : Delegate
    {
        for (var value = (int)min; value <= (int)max; value++)
        {
            var code = (RelayMessageCode)value;

            if (!handlers.TryGetValue(code, out var existing))
                continue;

            if (Delegate.Remove(existing, handler) is THandler remaining)
                handlers[code] = remaining;
            else
                handlers.TryRemove(code, out _);
        }
    }

    private static void RequireBuiltIn(RelayMessageCode min, RelayMessageCode max)
    {
        if (min < RelayMessageCode.MinBuiltInEvent || max > RelayMessageCode.MaxBuiltInEvent || min > max)
            throw new ArgumentOutOfRangeException(
                nameof(min), $"Built-in codes run from {nameof(RelayMessageCode.MinBuiltInEvent)} to "
                             + $"{nameof(RelayMessageCode.MaxBuiltInEvent)}; got {min} to {max}.");
    }

    private static void RequireCustom(RelayMessageCode min, RelayMessageCode max)
    {
        if (min < RelayMessageCode.MinAnyCustomEvent || min > max)
            throw new ArgumentOutOfRangeException(
                nameof(min), $"Mod codes start at {nameof(RelayMessageCode.MinAnyCustomEvent)}; got {min} to {max}.");
    }
}
