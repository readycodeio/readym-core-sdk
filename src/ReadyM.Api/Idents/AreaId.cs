using System;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;
using ReadyM.Native.Container;
using ReadyM.Native.Serialization;

namespace ReadyM.Api.Idents;

/// <summary>
/// Identifies an area: a part of the game world behind a loading screen, such as a whole map or a single interior.
/// </summary>
[DeriveJsonSerializable]
public partial struct AreaId : INetSerializable, IEquatable<AreaId>
{
    private NativeString256 _id;

    /// <summary>An area id from its native string.</summary>
    public AreaId(NativeString256 id)
    {
        _id = id;
    }
    
    /// <summary>An area id from its string form.</summary>
    public AreaId(string id)
    {
        _id = new NativeString256(id, true);
    }
    
    /// <summary>An area id from a number, stored as its decimal text.</summary>
    public AreaId(int id)
    {
        _id = new NativeString256(id.ToString(), true);
    }

    /// <summary>No area.</summary>
    public static AreaId Invalid => default;

    internal string RawValue => _id.ToString();

    public string ToManaged()
        => _id.ToManaged();

    public void Serialize(NetDataWriter writer)
    {
        _id.Serialize(writer);
    }

    public void Deserialize(NetDataReader reader)
    {
        _id.Deserialize(reader);
    }

    /// <summary>Whether both ids name the same area.</summary>
    public bool Equals(AreaId other)
        => _id == other._id;

    public override bool Equals(object? obj)
        => obj is AreaId other && Equals(other);

    public override int GetHashCode()
        => _id.GetHashCode();

    /// <summary>Whether both ids name the same area.</summary>
    public static bool operator ==(AreaId left, AreaId right)
        => left._id == right._id;

    /// <summary>Whether the ids name different areas.</summary>
    public static bool operator !=(AreaId left, AreaId right)
        => left._id != right._id;

    public override string ToString()
        => _id == Invalid._id ? "AreaId.Invalid" : $"AreaId[{_id}]";
}