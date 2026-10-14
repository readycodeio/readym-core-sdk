using System.Numerics;
using LiteNetLib.Utils;
using ReadyM.Api.Multiplayer.Serialization;
using Xunit;

namespace ReadyM.Api.Generators.Tests;

// The wire encodings behind [Quantize], [QuantizeAngle] and [HalfPrecision]
public sealed class QuantizationTests
{
    private const float WorldMin = -1048576f;
    private const float WorldMax = 1048576f;
    private const float WorldStep = (WorldMax - WorldMin) / 0xFFFFFE;

    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(24)]
    public void Both_ends_of_a_range_survive_exactly(int bits)
    {
        Assert.Equal(WorldMin, RoundTrip(WorldMin, WorldMin, WorldMax, bits));
        Assert.Equal(WorldMax, RoundTrip(WorldMax, WorldMin, WorldMax, bits));
        Assert.Equal(0f, RoundTrip(0f, 0f, 1f, bits));
        Assert.Equal(1f, RoundTrip(1f, 0f, 1f, bits));
    }

    [Fact]
    public void A_position_comes_back_within_half_a_step()
    {
        var random = new Random(1);
        for (var i = 0; i < 100_000; i++)
        {
            var value = (float)(random.NextDouble() * (WorldMax - WorldMin) + WorldMin);
            Assert.InRange(RoundTrip(value, WorldMin, WorldMax, 24) - value, -WorldStep / 2 - 0.07f, WorldStep / 2 + 0.07f);
        }
    }

    // The relay decodes and re-encodes, so a decoded value must encode to the step it came from. Across Tamriel's
    // exterior (about ±3.75 km) a float holds a position far more precisely than a step, so this is exact
    [Fact]
    public void A_decoded_position_within_the_world_encodes_to_the_same_step()
    {
        var first = Quantization.Quantize(-500_000f, WorldMin, WorldMax, 24);
        var last = Quantization.Quantize(500_000f, WorldMin, WorldMax, 24);
        for (var q = first; q <= last; q += 97)
        {
            var decoded = Quantization.Dequantize(q, WorldMin, WorldMax, 24);
            Assert.Equal(q, Quantization.Quantize(decoded, WorldMin, WorldMax, 24));
        }
    }

    [Fact]
    public void Out_of_range_values_clamp_to_the_ends()
    {
        Assert.Equal(1f, RoundTrip(3f, 0f, 1f, 8));
        Assert.Equal(0f, RoundTrip(-3f, 0f, 1f, 8));
        Assert.Equal(0f, RoundTrip(float.NaN, 0f, 1f, 8));
    }

    [Theory]
    [InlineData(8, 1)]
    [InlineData(16, 2)]
    [InlineData(24, 3)]
    public void A_quantized_number_takes_bits_over_eight_bytes(int bits, int bytes)
    {
        var writer = new NetDataWriter();
        Quantization.Write(writer, 0.5f, 0f, 1f, bits);
        Assert.Equal(bytes, writer.Length);

        writer.Reset();
        Quantization.Write(writer, new Vector3(0.1f, 0.2f, 0.3f), 0f, 1f, bits);
        Assert.Equal(3 * bytes, writer.Length);
    }

    [Fact]
    public void Values_in_the_same_step_count_as_unchanged()
    {
        // From the middle of a step a quarter step either way stays in it; from an arbitrary value it may not
        var center = Quantization.Dequantize(Quantization.Quantize(100f, WorldMin, WorldMax, 24), WorldMin, WorldMax, 24);
        Assert.True(Quantization.SameStep(center, center + WorldStep / 4, WorldMin, WorldMax, 24));
        Assert.True(Quantization.SameStep(center, center - WorldStep / 4, WorldMin, WorldMax, 24));
        Assert.False(Quantization.SameStep(100f, 100f + WorldStep * 2, WorldMin, WorldMax, 24));
        Assert.False(Quantization.SameStep(new Vector3(1, 2, 3), new Vector3(1, 2, 3 + WorldStep * 2), WorldMin, WorldMax, 24));
    }

    // An even step count puts the middle of a range on a step, not between two, so a value resting at zero and
    // jittering a little does not flip between steps
    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(24)]
    public void Zero_in_a_symmetric_range_is_exact_and_jitter_around_it_stays_in_its_step(int bits)
    {
        var step = 2f / ((1 << bits) - 2);

        Assert.Equal(0f, RoundTrip(0f, -1f, 1f, bits));
        Assert.True(Quantization.SameStep(0f, step / 4, -1f, 1f, bits));
        Assert.True(Quantization.SameStep(0f, -step / 4, -1f, 1f, bits));
        Assert.True(Quantization.SameStep(step / 4, -step / 4, -1f, 1f, bits));
    }

    [Fact]
    public void A_change_within_the_cutoff_is_no_change_even_across_a_step()
    {
        // Two values either side of a step boundary, 0.02 apart
        var boundary = Quantization.Dequantize(Quantization.Quantize(100f, WorldMin, WorldMax, 24), WorldMin, WorldMax, 24) + WorldStep / 2;
        Assert.False(Quantization.SameStep(boundary - 0.01f, boundary + 0.01f, WorldMin, WorldMax, 24));
        Assert.True(Quantization.Unchanged(boundary - 0.01f, boundary + 0.01f, WorldMin, WorldMax, 24));

        Assert.False(Quantization.SameHalf(1f, 1.05f));
        Assert.True(Quantization.UnchangedHalf(1f, 1.05f));
        Assert.True(Quantization.UnchangedHalf(new Vector3(1f, 2f, 3f), new Vector3(1.05f, 2.05f, 3f)));

        Assert.False(Quantization.SameAngleStep(45f, 45.05f, 16));
        Assert.True(Quantization.UnchangedAngle(45f, 45.05f, 16));
    }

    [Fact]
    public void A_change_past_the_cutoff_that_moves_the_encoding_is_a_change()
    {
        Assert.False(Quantization.Unchanged(100f, 100.5f, WorldMin, WorldMax, 24));
        Assert.False(Quantization.Unchanged(new Vector3(1f, 2f, 3f), new Vector3(1f, 2f, 3.2f), WorldMin, WorldMax, 24));
        Assert.False(Quantization.UnchangedHalf(1f, 1.2f));
        Assert.False(Quantization.UnchangedHalf(new Vector2(1f, 2f), new Vector2(1.2f, 2f)));
        Assert.False(Quantization.UnchangedAngle(45f, 45.2f, 16));
    }

    [Fact]
    public void A_change_past_the_cutoff_inside_one_step_is_no_change()
    {
        // An 8-bit step over -100..100 is about 0.79, so 0.3 from the middle of a step stays in it
        var center = Quantization.Dequantize(Quantization.Quantize(10f, -100f, 100f, 8), -100f, 100f, 8);
        Assert.True(Quantization.Unchanged(center, center + 0.3f, -100f, 100f, 8));
    }

    [Theory]
    [InlineData(10f, 350f, 20f)]
    [InlineData(179.95f, -179.98f, 0.07f)]
    [InlineData(-90f, 90f, 180f)]
    [InlineData(720f, 0f, 0f)]
    public void Angles_are_compared_around_the_circle(float a, float b, float distance)
    {
        Assert.Equal(distance, Quantization.AngleDistance(a, b), 3);
    }

    [Fact]
    public void A_turn_across_the_wrap_within_the_cutoff_is_no_change()
    {
        Assert.False(Quantization.SameAngleStep(179.95f, -179.98f, 16));
        Assert.True(Quantization.UnchangedAngle(179.95f, -179.98f, 16));
    }

    [Theory]
    [InlineData(190f, -170f)]
    [InlineData(-190f, 170f)]
    [InlineData(360f, 0f)]
    [InlineData(720f + 45f, 45f)]
    [InlineData(180f, -180f)]
    [InlineData(-90f, -90f)]
    public void An_angle_wraps_into_minus_180_to_180(float degrees, float expected)
    {
        const float halfStep = 360f / 65536 / 2;
        Assert.InRange(AngleRoundTrip(degrees, 16), expected - halfStep, expected + halfStep);
    }

    [Fact]
    public void A_decoded_angle_encodes_to_the_same_step()
    {
        for (uint q = 0; q < 1 << 16; q++)
            Assert.Equal(q, Quantization.QuantizeAngle(Quantization.DequantizeAngle(q, 16), 16));

        for (uint q = 0; q < 1 << 8; q++)
            Assert.Equal(q, Quantization.QuantizeAngle(Quantization.DequantizeAngle(q, 8), 8));
    }

    [Fact]
    public void An_angle_comes_back_within_half_a_step()
    {
        const float step = 360f / 65536;
        for (var degrees = -720f; degrees < 720f; degrees += 0.37f)
        {
            var back = AngleRoundTrip(degrees, 16);
            var difference = (back - degrees) % 360f;
            if (difference > 180f) difference -= 360f;
            if (difference < -180f) difference += 360f;
            Assert.InRange(difference, -step / 2 - 1e-3f, step / 2 + 1e-3f);
        }
    }

    // System.Half rounds to nearest, ties to even, which is what the wire uses; it is only missing on netstandard2.0
    [Fact]
    public void A_half_rounds_like_System_Half()
    {
        var random = new Random(2);
        for (var i = 0; i < 200_000; i++)
        {
            var value = (float)((random.NextDouble() * 2 - 1) * Math.Pow(2, random.Next(-30, 16)));
            Assert.Equal(BitConverter.HalfToUInt16Bits((Half)value), Quantization.ToHalfBits(value));
        }

        foreach (var value in new[] { 0f, -0f, 1f, -2f, 0.1f, 65504f, -65504f, 6.1035156e-5f, 5.9604645e-8f, 2.9802322e-8f })
            Assert.Equal(BitConverter.HalfToUInt16Bits((Half)value), Quantization.ToHalfBits(value));
    }

    [Fact]
    public void Every_half_reads_back_like_System_Half()
    {
        for (var bits = 0; bits <= ushort.MaxValue; bits++)
        {
            var expected = (float)BitConverter.UInt16BitsToHalf((ushort)bits);
            var actual = Quantization.FromHalfBits((ushort)bits);
            if (float.IsNaN(expected))
                Assert.True(float.IsNaN(actual));
            else
                Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void A_half_past_the_largest_finite_value_clamps_instead_of_turning_infinite()
    {
        Assert.Equal(65504f, Quantization.FromHalfBits(Quantization.ToHalfBits(1e6f)));
        Assert.Equal(-65504f, Quantization.FromHalfBits(Quantization.ToHalfBits(-70000f)));
    }

    // ---

    private static float RoundTrip(float value, float min, float max, int bits)
    {
        var writer = new NetDataWriter();
        Quantization.Write(writer, value, min, max, bits);
        return Quantization.ReadFloat(new NetDataReader(writer.CopyData()), min, max, bits);
    }

    private static float AngleRoundTrip(float degrees, int bits)
    {
        var writer = new NetDataWriter();
        Quantization.WriteAngle(writer, degrees, bits);
        return Quantization.ReadAngle(new NetDataReader(writer.CopyData()), bits);
    }
}
