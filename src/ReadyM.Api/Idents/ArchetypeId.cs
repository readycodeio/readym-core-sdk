using System;
using System.Runtime.InteropServices;
using LiteNetLib.Utils;

namespace ReadyM.Api.Idents;

/// <summary>
/// Represents a unique identifier for an archetype in the ECS.
/// Entities of a given archetype have a fixed set of components, never changed after creation.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ArchetypeId(byte id) : IEquatable<ArchetypeId>, INetSerializable
{
    public static ArchetypeId None => new(0);
    
    private byte _id = id;

    public bool Equals(ArchetypeId other)
        => _id == other._id;

    public override bool Equals(object? obj)
        => obj is ArchetypeId other && Equals(other);

    public override int GetHashCode()
        => _id.GetHashCode();

    public override string ToString()
        => $"ArchetypeId[{_id}]";

    public static bool operator ==(ArchetypeId left, ArchetypeId right)
        => left.Equals(right);

    public static bool operator !=(ArchetypeId left, ArchetypeId right)
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