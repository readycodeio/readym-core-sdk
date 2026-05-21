using System;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;
using Yooni.Native.Container;
using Yooni.Native.Serialization;

namespace ReadyM.Api.Idents;

/// <summary>
/// Identifies an area within the game world.
/// Areas can be main Chapter maps, sub-areas such as the Zodiac Village, or hidden boss arenas.
/// If going somewhere requires a loading screen, it's probably a different area.
/// </summary>
/// <param name="id">The underlying ID value. This is not guaranteed to be stable across game versions, and should not be used for anything other than debugging or logging purposes.</param>
[DeriveJsonSerializable]
public partial struct AreaId : INetSerializable, IEquatable<AreaId>
{
    private NativeString256 _id;

    public AreaId(NativeString256 id)
    {
        _id = id;
    }
    
    public AreaId(string id)
    {
        _id = new NativeString256(id, true);
    }
    
    public AreaId(int id)
    {
        _id = new NativeString256(id.ToString(), true);
    }

    public static AreaId Invalid => default;

    public void Serialize(NetDataWriter writer)
    {
        _id.Serialize(writer);
    }

    public void Deserialize(NetDataReader reader)
    {
        _id.Deserialize(reader);
    }

    public bool Equals(AreaId other)
        => _id == other._id;

    public override bool Equals(object? obj)
        => obj is AreaId other && Equals(other);

    public override int GetHashCode()
        => _id.GetHashCode();

    public static bool operator ==(AreaId left, AreaId right)
        => left._id == right._id;

    public static bool operator !=(AreaId left, AreaId right)
        => left._id != right._id;

    public override string ToString()
        => _id == Invalid._id ? "AreaId.Invalid" : $"AreaId[{_id}]";
}