using System;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Idents;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.ECS.Components;

[DeriveJsonSerializable]
public partial struct NetworkId(PlayerId creator, uint id) : IEquatable<NetworkId>, INetSerializable
{
    public PlayerId Creator { get; private set; } = creator; // 2 bytes
    public uint Id { get; private set; } = id; // 4 bytes

    public bool Equals(NetworkId other)
    {
        return Creator == other.Creator && Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is NetworkId other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Creator.GetHashCode() * 397) ^ Id.GetHashCode();
        }
    }

    public static bool operator ==(NetworkId left, NetworkId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NetworkId left, NetworkId right)
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
