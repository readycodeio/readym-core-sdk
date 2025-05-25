using System;
using Friflo.Engine.ECS;

namespace ReadyM.Api.Multiplayer;

public readonly struct NetworkIdComponent(short owner, uint id) : IEquatable<NetworkIdComponent>, IIndexedComponent<NetworkIdComponent>
{
    // TODO: ushort, for now it's short so that server can be -1
    // TODO: Replace LiteNetLib's peer ID with this for players
    public readonly short Owner = owner;
    public readonly uint Id = id; // per-owner unique ID

    [Obsolete]
    public static NetworkIdComponent FromPlayerPeerId(int peerId) => new(-1, (uint)peerId);

    public bool Equals(NetworkIdComponent other)
    {
        return Owner == other.Owner && Id == other.Id;
    }

    public NetworkIdComponent GetIndexedValue()
    {
        return this;
    }

    public override bool Equals(object? obj)
    {
        return obj is NetworkIdComponent other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Owner * 397) ^ (int)Id;
        }
    }

    public static bool operator ==(NetworkIdComponent left, NetworkIdComponent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NetworkIdComponent left, NetworkIdComponent right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"NetId[{Owner}, {Id}]";
    }
}