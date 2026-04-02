using System;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Idents;

/// <summary>
/// Identifies an area within the game world.
/// Areas can be main Chapter maps, sub-areas such as the Zodiac Village, or hidden boss arenas.
/// If going somewhere requires a loading screen, it's probably a different area.
/// </summary>
/// <param name="id">The underlying ID value. This is not guaranteed to be stable across game versions, and should not be used for anything other than debugging or logging purposes.</param>
[DeriveJsonSerializable]
public partial struct AreaId(ushort id) : INetSerializable, IEquatable<AreaId>
{
    private ushort _id = id;

    public static AreaId Invalid => default;
    public static AreaId Max => new(ushort.MaxValue);

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(_id);
    }

    public void Deserialize(NetDataReader reader)
    {
        _id = reader.GetUShort();
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