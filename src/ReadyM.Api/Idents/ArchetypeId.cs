using System;
using LiteNetLib.Utils;

namespace ReadyM.Api.Idents;

public struct ArchetypeId : IEquatable<ArchetypeId>, INetSerializable
{
    public static ArchetypeId None => new(0);
    
    private byte _id;
    
    internal ArchetypeId(byte id)
    {
        _id = id;
    }
    
    public bool Equals(ArchetypeId other)
    {
        return _id == other._id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ArchetypeId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static bool operator ==(ArchetypeId left, ArchetypeId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ArchetypeId left, ArchetypeId right)
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