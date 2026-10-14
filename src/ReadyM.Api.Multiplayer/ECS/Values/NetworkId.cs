using System;
using System.Runtime.InteropServices;
using LiteNetLib.Utils;
using ReadyM.Api.Idents;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.ECS.Values;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct NetworkId(PlayerId creator, uint id) : IEquatable<NetworkId>, INetSerializable
{
    // 1-5 bytes on the wire, 4 bytes in memory
    private VarUInt _id = id;
    public uint Id => _id.Value;

    // 1-3 bytes on the wire, 4 bytes in memory
    public PlayerId Creator { get; private set; } = creator;

    public bool Equals(NetworkId other)
        => Creator == other.Creator && Id == other.Id;

    public override bool Equals(object? obj)
        => obj is NetworkId other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            return (Creator.GetHashCode() * 397) ^ Id.GetHashCode();
        }
    }

    public static bool operator ==(NetworkId left, NetworkId right)
        => left.Equals(right);

    public static bool operator !=(NetworkId left, NetworkId right)
        => !left.Equals(right);

    public override string ToString()
        => $"NetId[{Creator}, {Id}]";

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(Creator);
        writer.Put(_id);
    }

    public void Deserialize(NetDataReader reader)
    {
        Creator = reader.Get<PlayerId>();
        _id = reader.Get<VarUInt>();
    }
}