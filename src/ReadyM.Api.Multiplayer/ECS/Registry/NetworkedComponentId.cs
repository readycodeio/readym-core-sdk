using System;
using System.Runtime.InteropServices;
using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct NetworkedComponentId(byte id) : IEquatable<NetworkedComponentId>, INetSerializable
{
    public static NetworkedComponentId None => new(0);
    
    private byte _id = id;

    public bool Equals(NetworkedComponentId other)
    {
        return _id == other._id;
    }

    public override bool Equals(object? obj)
    {
        return obj is NetworkedComponentId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static bool operator ==(NetworkedComponentId left, NetworkedComponentId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NetworkedComponentId left, NetworkedComponentId right)
    {
        return !left.Equals(right);
    }

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(_id);
    }
    public void Deserialize(NetDataReader reader)
    {
        _id = reader.GetByte();
    }
}