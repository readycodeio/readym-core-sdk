using System;

namespace ReadyM.Api;

public readonly struct ArchetypeId : IEquatable<ArchetypeId>
{
    private readonly int _id;
    
    internal ArchetypeId(int id)
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
        return _id;
    }

    public static bool operator ==(ArchetypeId left, ArchetypeId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ArchetypeId left, ArchetypeId right)
    {
        return !left.Equals(right);
    }
}