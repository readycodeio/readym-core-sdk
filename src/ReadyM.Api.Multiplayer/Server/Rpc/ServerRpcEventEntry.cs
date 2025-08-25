using System;
using ReadyM.Api.Multiplayer.Protocol.Enums;

namespace ReadyM.Api.Multiplayer.Server.Rpc;

public readonly struct ServerRpcEventEntry: IEquatable<ServerRpcEventEntry>, IComparable<ServerRpcEventEntry>
{
    public RelayMessageCode EventCode { get; }

    public ServerRpcEventEntry(RelayMessageCode eventCode)
    {
        if (eventCode < RelayMessageCode.MinServerRpcEvent || eventCode > RelayMessageCode.MaxServerRpcEvent)
            throw new ArgumentOutOfRangeException(nameof(eventCode), "Invalid server rpc event code");
        EventCode = eventCode;
    }

    public override bool Equals(object? obj)
    {
        return obj is ServerRpcEventEntry other && Equals(other);
    }

    public bool Equals(ServerRpcEventEntry other)
    {
        return EventCode == other.EventCode;
    }

    public override int GetHashCode()
    {
        return EventCode.GetHashCode();
    }

    public static bool operator ==(ServerRpcEventEntry left, ServerRpcEventEntry right) => left.Equals(right);
    public static bool operator !=(ServerRpcEventEntry left, ServerRpcEventEntry right) => !(left == right);

    public int CompareTo(ServerRpcEventEntry other) => EventCode.CompareTo(other.EventCode);
    public static bool operator <(ServerRpcEventEntry left, ServerRpcEventEntry right) => left.CompareTo(right) < 0;
    public static bool operator >(ServerRpcEventEntry left, ServerRpcEventEntry right) => left.CompareTo(right) > 0;
    public static bool operator <=(ServerRpcEventEntry left, ServerRpcEventEntry right) => left.CompareTo(right) <= 0;
    public static bool operator >=(ServerRpcEventEntry left, ServerRpcEventEntry right) => left.CompareTo(right) >= 0;
}
