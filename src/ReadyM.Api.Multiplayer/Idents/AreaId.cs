using System;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.Idents;

[DeriveJsonSerializable]
public partial struct AreaId(ushort id) : INetSerializable, IEquatable<AreaId>
{
    private ushort _id = id;

    /// <summary>
    /// Numeric identifier for a player. This can change over time, so it should not be used as a persistent identifier.
    /// Rely on the <see cref="PlayerId"/> as a unique identifier for the current session.
    /// </summary>
    public ushort RawValue => _id;

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
        => obj is PlayerId other && Equals(other);

    public override int GetHashCode()
        => _id.GetHashCode();

    public static bool operator ==(AreaId left, AreaId right)
        => left._id == right._id;

    public static bool operator !=(AreaId left, AreaId right)
        => left._id != right._id;

    public override string ToString()
        => _id == Invalid._id ? "AreaId.Invalid" : $"AreaId[{_id}]";
}