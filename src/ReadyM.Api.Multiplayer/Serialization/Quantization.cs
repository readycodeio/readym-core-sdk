using System;
using System.Numerics;
using System.Runtime.InteropServices;
using LiteNetLib.Utils;

namespace ReadyM.Api.Multiplayer.Serialization;

/// <exclude />
/// Wire encodings behind [Quantize], [QuantizeAngle] and [HalfPrecision], called from generated component code.
/// A field changes, and is sent, only when it moves past the plain fields' cutoff and its encoding changes too; the
/// Unchanged* checks decide that, the Same* checks compare encodings alone.
public static class Quantization
{
    /// The cutoff plain float fields use: a scalar change up to this is not a change.
    public const float ScalarDeadZone = 0.1f;

    /// The cutoff plain vector fields use, as a squared distance: a move up to 0.1 is not a change.
    public const float VectorDeadZoneSquared = 0.01f;

    // --- fixed-point range: 2^bits - 2 steps between min and max. An even count keeps both ends and the midpoint
    // exact, so in a symmetric range a value resting at zero does not sit on a rounding boundary and flicker

    public static uint Quantize(float value, float min, float max, int bits)
    {
        var steps = StepsOf(bits);
        if (float.IsNaN(value))
            return 0;

        var t = ((double)value - min) / ((double)max - min);
        if (t <= 0)
            return 0;
        if (t >= 1)
            return steps;

        return (uint)Math.Floor(t * steps + 0.5);
    }

    public static float Dequantize(uint quantized, float min, float max, int bits)
        => (float)(min + ((double)max - min) * quantized / StepsOf(bits));

    public static bool SameStep(float a, float b, float min, float max, int bits)
        => Quantize(a, min, max, bits) == Quantize(b, min, max, bits);

    public static bool SameStep(Vector2 a, Vector2 b, float min, float max, int bits)
        => SameStep(a.X, b.X, min, max, bits) && SameStep(a.Y, b.Y, min, max, bits);

    public static bool SameStep(Vector3 a, Vector3 b, float min, float max, int bits)
        => SameStep(a.X, b.X, min, max, bits) && SameStep(a.Y, b.Y, min, max, bits) && SameStep(a.Z, b.Z, min, max, bits);

    public static bool SameStep(Vector4 a, Vector4 b, float min, float max, int bits)
        => SameStep(a.X, b.X, min, max, bits) && SameStep(a.Y, b.Y, min, max, bits)
                                              && SameStep(a.Z, b.Z, min, max, bits) && SameStep(a.W, b.W, min, max, bits);

    public static void Write(NetDataWriter writer, float value, float min, float max, int bits)
        => PutBits(writer, Quantize(value, min, max, bits), bits);

    public static void Write(NetDataWriter writer, Vector2 value, float min, float max, int bits)
    {
        Write(writer, value.X, min, max, bits);
        Write(writer, value.Y, min, max, bits);
    }

    public static void Write(NetDataWriter writer, Vector3 value, float min, float max, int bits)
    {
        Write(writer, value.X, min, max, bits);
        Write(writer, value.Y, min, max, bits);
        Write(writer, value.Z, min, max, bits);
    }

    public static void Write(NetDataWriter writer, Vector4 value, float min, float max, int bits)
    {
        Write(writer, value.X, min, max, bits);
        Write(writer, value.Y, min, max, bits);
        Write(writer, value.Z, min, max, bits);
        Write(writer, value.W, min, max, bits);
    }

    public static float ReadFloat(NetDataReader reader, float min, float max, int bits)
        => Dequantize(GetBits(reader, bits), min, max, bits);

    public static Vector2 ReadVector2(NetDataReader reader, float min, float max, int bits)
    {
        var x = ReadFloat(reader, min, max, bits);
        var y = ReadFloat(reader, min, max, bits);
        return new Vector2(x, y);
    }

    public static Vector3 ReadVector3(NetDataReader reader, float min, float max, int bits)
    {
        var x = ReadFloat(reader, min, max, bits);
        var y = ReadFloat(reader, min, max, bits);
        var z = ReadFloat(reader, min, max, bits);
        return new Vector3(x, y, z);
    }

    public static Vector4 ReadVector4(NetDataReader reader, float min, float max, int bits)
    {
        var x = ReadFloat(reader, min, max, bits);
        var y = ReadFloat(reader, min, max, bits);
        var z = ReadFloat(reader, min, max, bits);
        var w = ReadFloat(reader, min, max, bits);
        return new Vector4(x, y, z, w);
    }

    // --- angles in degrees: 2^bits steps around the circle, wrapping

    public static uint QuantizeAngle(float degrees, int bits)
    {
        if (float.IsNaN(degrees) || float.IsInfinity(degrees))
            return 0;

        var turns = (double)degrees / 360.0;
        turns -= Math.Floor(turns);
        var steps = 1u << bits;
        return (uint)Math.Floor(turns * steps + 0.5) & (steps - 1);
    }

    public static float DequantizeAngle(uint quantized, int bits)
    {
        var steps = 1u << bits;
        if (quantized >= steps / 2)
            return (float)(((double)quantized - steps) * 360.0 / steps);

        return (float)((double)quantized * 360.0 / steps);
    }

    public static bool SameAngleStep(float a, float b, int bits)
        => QuantizeAngle(a, bits) == QuantizeAngle(b, bits);

    public static void WriteAngle(NetDataWriter writer, float degrees, int bits)
        => PutBits(writer, QuantizeAngle(degrees, bits), bits);

    public static float ReadAngle(NetDataReader reader, int bits)
        => DequantizeAngle(GetBits(reader, bits), bits);

    // --- 16-bit floats

    public static bool SameHalf(float a, float b)
        => ToHalfBits(a) == ToHalfBits(b);

    public static bool SameHalf(Vector2 a, Vector2 b)
        => SameHalf(a.X, b.X) && SameHalf(a.Y, b.Y);

    public static bool SameHalf(Vector3 a, Vector3 b)
        => SameHalf(a.X, b.X) && SameHalf(a.Y, b.Y) && SameHalf(a.Z, b.Z);

    public static bool SameHalf(Vector4 a, Vector4 b)
        => SameHalf(a.X, b.X) && SameHalf(a.Y, b.Y) && SameHalf(a.Z, b.Z) && SameHalf(a.W, b.W);

    public static void WriteHalf(NetDataWriter writer, float value)
        => writer.Put(ToHalfBits(value));

    public static void WriteHalf(NetDataWriter writer, Vector2 value)
    {
        WriteHalf(writer, value.X);
        WriteHalf(writer, value.Y);
    }

    public static void WriteHalf(NetDataWriter writer, Vector3 value)
    {
        WriteHalf(writer, value.X);
        WriteHalf(writer, value.Y);
        WriteHalf(writer, value.Z);
    }

    public static void WriteHalf(NetDataWriter writer, Vector4 value)
    {
        WriteHalf(writer, value.X);
        WriteHalf(writer, value.Y);
        WriteHalf(writer, value.Z);
        WriteHalf(writer, value.W);
    }

    public static float ReadHalf(NetDataReader reader)
        => FromHalfBits(reader.GetUShort());

    public static Vector2 ReadHalfVector2(NetDataReader reader)
    {
        var x = ReadHalf(reader);
        var y = ReadHalf(reader);
        return new Vector2(x, y);
    }

    public static Vector3 ReadHalfVector3(NetDataReader reader)
    {
        var x = ReadHalf(reader);
        var y = ReadHalf(reader);
        var z = ReadHalf(reader);
        return new Vector3(x, y, z);
    }

    public static Vector4 ReadHalfVector4(NetDataReader reader)
    {
        var x = ReadHalf(reader);
        var y = ReadHalf(reader);
        var z = ReadHalf(reader);
        var w = ReadHalf(reader);
        return new Vector4(x, y, z, w);
    }

    /// Rounds to the nearest 16-bit float, ties to even. Values past the largest finite half clamp to it rather than
    /// becoming infinity, so a spike cannot poison an animation input.
    public static ushort ToHalfBits(float value)
    {
        var bits = new FloatBits { Float = value }.Bits;
        var sign = (bits >> 16) & 0x8000u;
        var exponent = (int)((bits >> 23) & 0xFF);
        var mantissa = bits & 0x7FFFFFu;

        if (exponent == 0xFF)
            return (ushort)(sign | 0x7C00u | (mantissa != 0 ? 0x200u : 0u));

        var halfExponent = exponent - 127 + 15;
        if (halfExponent >= 0x1F)
            return (ushort)(sign | 0x7BFFu);

        if (halfExponent <= 0)
        {
            if (halfExponent < -10)
                return (ushort)sign;

            mantissa |= 0x800000u;
            var shift = 14 - halfExponent;
            var subnormal = mantissa >> shift;
            var remainder = mantissa & ((1u << shift) - 1);
            var halfway = 1u << (shift - 1);
            if (remainder > halfway || (remainder == halfway && (subnormal & 1) != 0))
                subnormal++;

            return (ushort)(sign | subnormal);
        }

        var half = sign | ((uint)halfExponent << 10) | (mantissa >> 13);
        var rest = mantissa & 0x1FFFu;
        if (rest > 0x1000u || (rest == 0x1000u && (half & 1) != 0))
            half++;

        if ((half & 0x7FFFu) >= 0x7C00u)
            half = sign | 0x7BFFu;

        return (ushort)half;
    }

    public static float FromHalfBits(ushort half)
    {
        var sign = (uint)(half & 0x8000) << 16;
        var exponent = (half >> 10) & 0x1F;
        var mantissa = (uint)(half & 0x3FF);

        uint bits;
        if (exponent == 0)
        {
            if (mantissa == 0)
            {
                bits = sign;
            }
            else
            {
                exponent = 1;
                while ((mantissa & 0x400) == 0)
                {
                    mantissa <<= 1;
                    exponent--;
                }

                mantissa &= 0x3FF;
                bits = sign | ((uint)(exponent - 15 + 127) << 23) | (mantissa << 13);
            }
        }
        else if (exponent == 0x1F)
        {
            bits = sign | 0x7F800000u | (mantissa << 13);
        }
        else
        {
            bits = sign | ((uint)(exponent - 15 + 127) << 23) | (mantissa << 13);
        }

        return new FloatBits { Bits = bits }.Float;
    }

    // --- whether a write counts as a change

    public static bool Unchanged(float a, float b, float min, float max, int bits)
        => Math.Abs(a - b) <= ScalarDeadZone || SameStep(a, b, min, max, bits);

    public static bool Unchanged(Vector2 a, Vector2 b, float min, float max, int bits)
        => Vector2.DistanceSquared(a, b) <= VectorDeadZoneSquared || SameStep(a, b, min, max, bits);

    public static bool Unchanged(Vector3 a, Vector3 b, float min, float max, int bits)
        => Vector3.DistanceSquared(a, b) <= VectorDeadZoneSquared || SameStep(a, b, min, max, bits);

    public static bool Unchanged(Vector4 a, Vector4 b, float min, float max, int bits)
        => Vector4.DistanceSquared(a, b) <= VectorDeadZoneSquared || SameStep(a, b, min, max, bits);

    public static bool UnchangedAngle(float a, float b, int bits)
        => AngleDistance(a, b) <= ScalarDeadZone || SameAngleStep(a, b, bits);

    public static bool UnchangedHalf(float a, float b)
        => Math.Abs(a - b) <= ScalarDeadZone || SameHalf(a, b);

    public static bool UnchangedHalf(Vector2 a, Vector2 b)
        => Vector2.DistanceSquared(a, b) <= VectorDeadZoneSquared || SameHalf(a, b);

    public static bool UnchangedHalf(Vector3 a, Vector3 b)
        => Vector3.DistanceSquared(a, b) <= VectorDeadZoneSquared || SameHalf(a, b);

    public static bool UnchangedHalf(Vector4 a, Vector4 b)
        => Vector4.DistanceSquared(a, b) <= VectorDeadZoneSquared || SameHalf(a, b);

    /// How far apart two angles in degrees are around the circle, from 0 to 180.
    public static float AngleDistance(float a, float b)
    {
        var difference = Math.Abs(((double)a - b) % 360.0);
        return (float)(difference > 180.0 ? 360.0 - difference : difference);
    }

    // ---

    private static uint StepsOf(int bits)
        => bits switch
        {
            8 => 0xFEu,
            16 => 0xFFFEu,
            24 => 0xFFFFFEu,
            _ => throw new ArgumentOutOfRangeException(nameof(bits), bits, "Quantized fields use 8, 16 or 24 bits."),
        };

    private static void PutBits(NetDataWriter writer, uint value, int bits)
    {
        switch (bits)
        {
            case 8:
                writer.Put((byte)value);
                break;
            case 16:
                writer.Put((ushort)value);
                break;
            case 24:
                writer.Put((ushort)(value & 0xFFFF));
                writer.Put((byte)(value >> 16));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(bits), bits, "Quantized fields use 8, 16 or 24 bits.");
        }
    }

    private static uint GetBits(NetDataReader reader, int bits)
        => bits switch
        {
            8 => reader.GetByte(),
            16 => reader.GetUShort(),
            24 => reader.GetUShort() | ((uint)reader.GetByte() << 16),
            _ => throw new ArgumentOutOfRangeException(nameof(bits), bits, "Quantized fields use 8, 16 or 24 bits."),
        };

    [StructLayout(LayoutKind.Explicit)]
    private struct FloatBits
    {
        [FieldOffset(0)]
        public float Float;

        [FieldOffset(0)]
        public uint Bits;
    }
}