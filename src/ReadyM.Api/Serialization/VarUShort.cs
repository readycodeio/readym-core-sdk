using System;
using LiteNetLib.Utils;

namespace ReadyM.Api.Serialization;

/// Variable-length unsigned short.
/// Most significant bit of each byte is used as a continuation flag.
/// The remaining 7 bits are used to store the value.
[DeriveJsonSerializable]
public partial struct VarUShort(ushort value) : INetSerializable, IEquatable<VarUShort>
{
    public const int MaxSize = 3;

    public ushort Value { get; private set; } = value;

    /// How many bytes the value takes on the wire.
    public static int SizeOf(ushort value)
        => value switch
        {
            < 1 << 7 => 1,
            < 1 << 14 => 2,
            _ => 3,
        };

    public void Serialize(NetDataWriter writer)
        => writer.PutVarUShort(Value);

    public void Deserialize(NetDataReader reader)
        => Value = reader.GetVarUShort();

    public static implicit operator ushort(VarUShort value)
        => value.Value;

    public static implicit operator VarUShort(ushort value)
        => new(value);

    public bool Equals(VarUShort other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is VarUShort other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static bool operator ==(VarUShort left, VarUShort right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(VarUShort left, VarUShort right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
        => Value.ToString();
}
