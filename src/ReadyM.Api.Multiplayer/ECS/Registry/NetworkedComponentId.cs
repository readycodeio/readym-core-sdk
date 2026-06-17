using System;
using System.Runtime.InteropServices;
using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct NetworkedComponentId(byte id) : IEquatable<NetworkedComponentId>, INetSerializable
{
    public static NetworkedComponentId None => new(0);
    
    private byte _id = id;

    public bool Equals(NetworkedComponentId other)
        => _id == other._id;

    public override bool Equals(object? obj)
        => obj is NetworkedComponentId other && Equals(other);

    public override int GetHashCode()
        => _id.GetHashCode();

    public override string ToString()
        => $"NetworkedComponentId({_id})";

    public static bool operator ==(NetworkedComponentId left, NetworkedComponentId right)
        => left.Equals(right);

    public static bool operator !=(NetworkedComponentId left, NetworkedComponentId right)
        => !left.Equals(right);

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(_id);
    }
    
    public void Deserialize(NetDataReader reader)
    {
        _id = reader.GetByte();
    }
}