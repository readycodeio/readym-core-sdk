using System;
using System.Runtime.InteropServices;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.ECS.Registry;

namespace ReadyM.Api.Multiplayer.ECS.Values;

/// <summary>
/// A set of networked components, one bit per <see cref="NetworkedComponentId"/>. The ids are positional and shared by
/// the server and its clients, so a mask means the same components on both.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct NetworkedComponentMask : INetSerializable, IEquatable<NetworkedComponentMask>
{
    private ulong _bits0;
    private ulong _bits1;
    private ulong _bits2;
    private ulong _bits3;

    public readonly bool IsEmpty => (_bits0 | _bits1 | _bits2 | _bits3) == 0;

    internal readonly bool Contains(NetworkedComponentId id)
        => (Word(id.RawValue) & Bit(id.RawValue)) != 0;

    internal readonly NetworkedComponentMask With(NetworkedComponentId id)
    {
        var copy = this;
        var bit = Bit(id.RawValue);
        switch (id.RawValue >> 6)
        {
            case 0: copy._bits0 |= bit; break;
            case 1: copy._bits1 |= bit; break;
            case 2: copy._bits2 |= bit; break;
            default: copy._bits3 |= bit; break;
        }

        return copy;
    }

    private readonly ulong Word(byte id) => (id >> 6) switch
    {
        0 => _bits0,
        1 => _bits1,
        2 => _bits2,
        _ => _bits3,
    };

    private static ulong Bit(byte id) => 1UL << (id & 63);

    public void Serialize(NetDataWriter writer)
    {
        writer.Put(_bits0);
        writer.Put(_bits1);
        writer.Put(_bits2);
        writer.Put(_bits3);
    }

    public void Deserialize(NetDataReader reader)
    {
        _bits0 = reader.GetULong();
        _bits1 = reader.GetULong();
        _bits2 = reader.GetULong();
        _bits3 = reader.GetULong();
    }

    public readonly bool Equals(NetworkedComponentMask other)
        => _bits0 == other._bits0 && _bits1 == other._bits1 && _bits2 == other._bits2 && _bits3 == other._bits3;

    public override readonly bool Equals(object? obj)
        => obj is NetworkedComponentMask other && Equals(other);

    public override readonly int GetHashCode()
        => (_bits0 ^ _bits1 ^ _bits2 ^ _bits3).GetHashCode();

    public static bool operator ==(NetworkedComponentMask left, NetworkedComponentMask right)
        => left.Equals(right);

    public static bool operator !=(NetworkedComponentMask left, NetworkedComponentMask right)
        => !left.Equals(right);
}
