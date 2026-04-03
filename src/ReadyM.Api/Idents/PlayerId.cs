using System;
using System.Runtime.InteropServices;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;

namespace ReadyM.Api.Idents;

/// <summary>
/// A unique identifier for a player in the current session.
/// This is not a persistent identifier and can change over time, especially if players disconnect and reconnect.
/// It should be used for identifying players during the current session, but not for long-term storage or cross-session identification.
/// </summary>
[DeriveJsonSerializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct PlayerId : INetSerializable, IEquatable<PlayerId>
{
    private ushort _id;

    internal PlayerId(ushort id)
    {
        _id = id;
    }

    internal ushort RawValue => _id;

    /// <summary>
    /// The PlayerId representing the server itself.
    /// This can be used to identify actions or messages that originate from the server rather than any specific player.
    /// </summary>
    public static PlayerId Server => default;

    /// <summary>
    /// An invalid PlayerId, which can be used to represent the absence of a player or an uninitialized state.
    /// </summary>
    public static PlayerId Invalid => default;
    
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
        => _id == Invalid._id ? "PlayerId.Server" : $"PlayerId[{_id}]";
}