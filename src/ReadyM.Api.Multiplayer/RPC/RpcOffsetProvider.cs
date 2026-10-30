using System;
using ReadyM.Api.Multiplayer.Protocol.Enums;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.RPC;

/// <summary>
/// Hands out the codes from <see cref="RelayMessageCode.MinAnyCustomEvent"/> up,
/// one contract set at a time, and remembers which sets are relayed between clients.
/// </summary>
internal sealed class RpcOffsetProvider
{
    /// <summary>How many codes we can assign to mods. The rest are reserved for the engine.</summary>
    private const int Capacity = ushort.MaxValue - (int)RelayMessageCode.MinAnyCustomEvent;

    /// <summary>The codes relayed between clients, as [start, end) ranges. Replaced whole, so a reader never sees it change.</summary>
    private (RelayMessageCode Start, RelayMessageCode End)[] _relayedBetweenClients = [];

    public VarUShort CurrentOffset { get; private set; }

    /// <exception cref="InvalidOperationException">
    /// The sets loaded together need more codes than the space holds.
    /// </exception>
    public VarUShort GetNextOffset(ushort events, MessageRouting routing)
    {
        if (CurrentOffset + events > Capacity)
            throw new InvalidOperationException(
                $"The custom code space holds {Capacity} codes, {CurrentOffset} of them already taken, and "
                + $"this contract set needs {events} more. Load fewer mods, or fewer contracts per mod.");

        var offset = CurrentOffset;
        CurrentOffset = (ushort)(CurrentOffset + events);

        if (routing == MessageRouting.BetweenClients && events > 0)
        {
            var start = (RelayMessageCode)((int)RelayMessageCode.MinAnyCustomEvent + offset);
            _relayedBetweenClients = [.. _relayedBetweenClients, (start, start + events)];
        }

        return offset;
    }

    /// <summary>Whether the code belongs to a set relayed between clients, so it arrives with the sender's header.</summary>
    public bool IsRelayedBetweenClients(RelayMessageCode code)
    {
        foreach (var (start, end) in _relayedBetweenClients)
            if (code >= start && code < end)
                return true;

        return false;
    }
}

/// <summary>Where a message's code is handled.</summary>
internal enum MessageRouting
{
    /// <summary>Sent to the server: relay code followed by payload.</summary>
    ToServer,

    /// <summary>Relayed by the server to other clients: relay code, header, payload.</summary>
    BetweenClients,
}
