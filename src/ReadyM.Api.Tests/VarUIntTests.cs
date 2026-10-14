using System.IO;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;
using Xunit;

namespace ReadyM.Api.Tests;

// Seven bits a byte, least significant group first, the top bit set on every byte but the last
public class VarUIntTests
{
    public static TheoryData<uint, byte[]> Encodings => new()
    {
        { 0u, [0x00] },
        { 1u, [0x01] },
        { 127u, [0x7F] },
        { 128u, [0x80, 0x01] },
        { 300u, [0xAC, 0x02] },
        { 16_383u, [0xFF, 0x7F] },
        { 16_384u, [0x80, 0x80, 0x01] },
        { 2_097_151u, [0xFF, 0xFF, 0x7F] },
        { 2_097_152u, [0x80, 0x80, 0x80, 0x01] },
        { 268_435_455u, [0xFF, 0xFF, 0xFF, 0x7F] },
        { 268_435_456u, [0x80, 0x80, 0x80, 0x80, 0x01] },
        { 0x12345678u, [0xF8, 0xAC, 0xD1, 0x91, 0x01] },
        { uint.MaxValue, [0xFF, 0xFF, 0xFF, 0xFF, 0x0F] },
    };

    [Theory]
    [MemberData(nameof(Encodings))]
    public void Writes_exactly_these_bytes(uint value, byte[] expected)
    {
        Assert.Equal(expected, Write(value));
    }

    [Theory]
    [MemberData(nameof(Encodings))]
    public void Reads_exactly_these_bytes_back_and_stops_after_them(uint value, byte[] encoded)
    {
        var reader = Reader([.. encoded, 0xAB]);

        Assert.Equal(value, reader.GetVarUInt());
        Assert.Equal(0xAB, reader.GetByte());
        Assert.True(reader.EndOfData);
    }

    [Theory]
    [MemberData(nameof(Encodings))]
    public void SizeOf_is_the_length_written(uint value, byte[] encoded)
    {
        Assert.Equal(encoded.Length, VarUInt.SizeOf(value));
    }

    [Theory]
    [InlineData(0u, 127u, 1)]
    [InlineData(128u, 16_383u, 2)]
    [InlineData(16_384u, 2_097_151u, 3)]
    [InlineData(2_097_152u, 268_435_455u, 4)]
    [InlineData(268_435_456u, uint.MaxValue, 5)]
    public void Every_length_starts_and_ends_where_seven_bit_groups_say(uint first, uint last, int size)
    {
        Assert.Equal(size, VarUInt.SizeOf(first));
        Assert.Equal(size, VarUInt.SizeOf(last));
        Assert.Equal(size, Write(first).Length);
        Assert.Equal(size, Write(last).Length);
        Assert.True(size <= VarUInt.MaxSize);
    }

    [Fact]
    public void Every_value_up_to_seventy_thousand_round_trips()
    {
        for (var value = 0u; value <= 70_000u; value++)
            AssertRoundTrips(value);
    }

    [Fact]
    public void Every_power_of_two_and_its_neighbours_round_trip()
    {
        for (var bit = 0; bit < 32; bit++)
        {
            var power = 1u << bit;
            AssertRoundTrips(power - 1);
            AssertRoundTrips(power);
            AssertRoundTrips(power + 1);
        }

        AssertRoundTrips(uint.MaxValue - 1);
        AssertRoundTrips(uint.MaxValue);
    }

    [Fact]
    public void Random_values_of_every_length_round_trip()
    {
        // A random bit length first, so the five sizes are hit about evenly rather than almost always five bytes
        var random = new Random(20261013);
        for (var i = 0; i < 200_000; i++)
        {
            var bits = random.Next(0, 33);
            var value = bits == 0 ? 0u : (uint)random.NextInt64(0, 1L << bits);
            AssertRoundTrips(value);
        }
    }

    [Fact]
    public void Values_back_to_back_read_back_in_order()
    {
        var values = new[] { 0u, 127u, 128u, 16_384u, uint.MaxValue, 5u, 268_435_456u, 300u };
        var writer = new NetDataWriter();
        foreach (var value in values)
            writer.PutVarUInt(value);

        var reader = new NetDataReader(writer.CopyData());
        foreach (var value in values)
            Assert.Equal(value, reader.GetVarUInt());
        Assert.True(reader.EndOfData);
    }

    [Fact]
    public void Only_the_last_byte_lacks_the_top_bit_and_it_is_never_a_wasted_zero()
    {
        var random = new Random(7);
        for (var i = 0; i < 50_000; i++)
        {
            var value = (uint)random.NextInt64(1, 1L << 32);
            var bytes = Write(value);

            Assert.All(bytes[..^1], b => Assert.True((b & 0x80) != 0));
            Assert.True((bytes[^1] & 0x80) == 0);
            Assert.NotEqual(0, bytes[^1]);
        }
    }

    [Fact]
    public void Writing_advances_the_writer_by_exactly_the_size()
    {
        var writer = new NetDataWriter();
        writer.Put(0x5Au);

        writer.PutVarUInt(2_097_152u);

        Assert.Equal(sizeof(uint) + 4, writer.Length);
    }

    // A longer form than needed still means the same number; only past 32 bits is it refused
    [Theory]
    [InlineData(0u, new byte[] { 0x80, 0x00 })]
    [InlineData(1u, new byte[] { 0x81, 0x80, 0x00 })]
    [InlineData(127u, new byte[] { 0xFF, 0x80, 0x80, 0x80, 0x00 })]
    public void A_longer_form_than_needed_still_reads(uint value, byte[] encoded)
    {
        var reader = Reader(encoded);

        Assert.Equal(value, reader.GetVarUInt());
        Assert.True(reader.EndOfData);
    }

    // A fifth byte with the top bit set asks for a sixth, whether or not one is there
    [Theory]
    [InlineData((byte)0x80, true)]
    [InlineData((byte)0x8F, true)]
    [InlineData((byte)0xFF, true)]
    [InlineData((byte)0x80, false)]
    public void A_sixth_byte_is_refused(byte fifth, bool sixthPresent)
    {
        var reader = sixthPresent ? Reader(0x80, 0x80, 0x80, 0x80, fifth, 0x00) : Reader(0x80, 0x80, 0x80, 0x80, fifth);

        var error = Assert.Throws<InvalidDataException>(() => reader.GetVarUInt());
        Assert.Contains("more than 5 bytes", error.Message);
    }

    [Theory]
    [InlineData((byte)0x10)]
    [InlineData((byte)0x40)]
    [InlineData((byte)0x7F)]
    public void A_fifth_byte_past_32_bits_is_refused(byte fifth)
    {
        var reader = Reader(0xFF, 0xFF, 0xFF, 0xFF, fifth, 0x00);

        var error = Assert.Throws<InvalidDataException>(() => reader.GetVarUInt());
        Assert.Contains("32 bits", error.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void A_stream_that_ends_mid_varint_is_refused(int length)
    {
        var reader = Reader(Write(uint.MaxValue)[..length]);

        var error = Assert.Throws<InvalidDataException>(() => reader.GetVarUInt());
        Assert.Contains("ends", error.Message);
    }

    [Fact]
    public void An_empty_stream_is_refused()
    {
        Assert.Throws<InvalidDataException>(() => Reader().GetVarUInt());
    }

    [Theory]
    [MemberData(nameof(Encodings))]
    public void As_a_serializable_it_writes_and_reads_the_same_bytes(uint value, byte[] encoded)
    {
        var writer = new NetDataWriter();
        writer.Put(new VarUInt(value));

        Assert.Equal(encoded, writer.CopyData());

        var reader = Reader(encoded);
        Assert.Equal(value, reader.Get<VarUInt>().Value);
        Assert.True(reader.EndOfData);
    }

    [Fact]
    public void It_converts_to_and_from_uint()
    {
        VarUInt fromUInt = 268_435_456u;
        uint toUInt = new VarUInt(300u);

        Assert.Equal(268_435_456u, fromUInt.Value);
        Assert.Equal(300u, toUInt);
        Assert.Equal(0u, default(VarUInt).Value);
    }

    private static NetDataReader Reader(params byte[] bytes)
        => new(bytes);

    private static byte[] Write(uint value)
    {
        var writer = new NetDataWriter();
        writer.PutVarUInt(value);
        return writer.CopyData();
    }

    private static void AssertRoundTrips(uint value)
    {
        var bytes = Write(value);
        Assert.Equal(VarUInt.SizeOf(value), bytes.Length);

        var reader = new NetDataReader(bytes);
        Assert.Equal(value, reader.GetVarUInt());
        Assert.True(reader.EndOfData);
    }
}
