using System;
using Friflo.Engine.ECS;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[DeriveJsonSerializable]
public partial struct NetworkIdComponent(PlayerId creator, uint id) : IEquatable<NetworkIdComponent>, IIndexedComponent<NetworkIdComponent>, INetSerializable
{
    public PlayerId Creator { get; private set; } = creator; // 2 bytes
    public uint Id { get; private set; } = id; // 4 bytes

    [Obsolete("uint.MaxValue indicates that this is not a player-owned monster, but the player himself. Will be removed once we add an archetype for player data.")]
    public static NetworkIdComponent FromPlayerId(PlayerId playerId) => new(playerId, uint.MaxValue);

    public bool Equals(NetworkIdComponent other)
    {
        return Creator == other.Creator && Id == other.Id;
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
            return (Creator.GetHashCode() * 397) ^ Id.GetHashCode();
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
        return $"NetId[{Creator}, {Id}]";
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Creator);
        writer.Put(Id);
    }

    public void Deserialize(NetDataReader reader)
    {
        Creator = reader.Get<PlayerId>();
        Id = reader.GetUInt();
    }
}
