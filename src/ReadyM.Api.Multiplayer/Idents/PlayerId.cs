using System;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Multiplayer.Idents;

[DeriveJsonSerializable]
public partial struct PlayerId(ushort id) : INetSerializable, IEquatable<PlayerId>
{
    private ushort _id = id;

    /// <summary>
    /// Numeric identifier for a player. This can change over time, so it should not be used as a persistent identifier.
    /// Rely on the <see cref="PlayerId"/> as a unique identifier for the current session.
    /// </summary>
    public ushort RawValue => _id;

    public static PlayerId Invalid => default;
    public static PlayerId Server => default;
    public static PlayerId Max => new(ushort.MaxValue);

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(_id);
    }

    public void Deserialize(NetDataReader reader)
    {
        _id = reader.GetUShort();
    }

    public bool Equals(PlayerId other)
        => _id == other._id;

    public override bool Equals(object? obj)
        => obj is PlayerId other && Equals(other);

    public override int GetHashCode()
        => _id.GetHashCode();

    public static bool operator ==(PlayerId left, PlayerId right)
        => left._id == right._id;

    public static bool operator !=(PlayerId left, PlayerId right)
        => left._id != right._id;

    public override string ToString()
        => _id == Invalid._id ? "PlayerId.Invalid" : $"PlayerId[{_id}]";
}
