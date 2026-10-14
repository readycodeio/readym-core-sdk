using System.IO;
using LiteNetLib.Utils;
using ReadyM.Api.Serialization;
using Xunit;

namespace ReadyM.Api.Tests;

// Seven bits a byte, least significant group first, the top bit set on every byte but the last; three bytes at most
public class VarUShortTests
{
    public static TheoryData<ushort, byte[]> Encodings => new()
    {
        { 0, [0x00] },
        { 1, [0x01] },
        { 127, [0x7F] },
        { 128, [0x80, 0x01] },
        { 300, [0xAC, 0x02] },
        { 0x1234, [0xB4, 0x24] },
        { 16_383, [0xFF, 0x7F] },
        { 16_384, [0x80, 0x80, 0x01] },
        { 0xABCD, [0xCD, 0xD7, 0x02] },
        { ushort.MaxValue, [0xFF, 0xFF, 0x03] },
    };

    [Theory]
    [MemberData(nameof(Encodings))]
    public void Writes_exactly_these_bytes(ushort value, byte[] expected)
    {
        Assert.Equal(expected, Write(value));
    }

    [Theory]
    [MemberData(nameof(Encodings))]
    public void Reads_exactly_these_bytes_back_and_stops_after_them(ushort value, byte[] encoded)
    {
        var reader = Reader([.. encoded, 0xAB]);

        Assert.Equal(value, reader.GetVarUShort());
        Assert.Equal(0xAB, reader.GetByte());
        Assert.True(reader.EndOfData);
    }

    [Theory]
    [MemberData(nameof(Encodings))]
    public void SizeOf_is_the_length_written(ushort value, byte[] encoded)
    {
        Assert.Equal(encoded.Length, VarUShort.SizeOf(value));
    }

    [Theory]
    [InlineData((ushort)0, (ushort)127, 1)]
    [InlineData((ushort)128, (ushort)16_383, 2)]
    [InlineData((ushort)16_384, ushort.MaxValue, 3)]
    public void Every_length_starts_and_ends_where_seven_bit_groups_say(ushort first, ushort last, int size)
    {
        Assert.Equal(size, VarUShort.SizeOf(first));
        Assert.Equal(size, VarUShort.SizeOf(last));
        Assert.Equal(size, Write(first).Length);
        Assert.Equal(size, Write(last).Length);
        Assert.True(size <= VarUShort.MaxSize);
    }

    [Fact]
    public void Every_value_round_trips()
    {
        for (var value = 0; value <= ushort.MaxValue; value++)
            AssertRoundTrips((ushort)value);
    }

    // The same encoding as a VarUInt, so either can read what the other wrote while the value fits
    [Fact]
    public void Every_value_is_written_exactly_as_a_VarUInt_writes_it()
    {
        for (var value = 0; value <= ushort.MaxValue; value++)
        {
            var asUInt = new NetDataWriter();
            asUInt.PutVarUInt((uint)value);

            Assert.Equal(asUInt.CopyData(), Write((ushort)value));
        }
    }

    [Fact]
    public void Values_back_to_back_read_back_in_order()
    {
        var values = new ushort[] { 0, 127, 128, 16_384, ushort.MaxValue, 5, 300, 16_383 };
        var writer = new NetDataWriter();
        foreach (var value in values)
            writer.PutVarUShort(value);

        var reader = new NetDataReader(writer.CopyData());
        foreach (var value in values)
            Assert.Equal(value, reader.GetVarUShort());
        Assert.True(reader.EndOfData);
    }

    [Fact]
    public void Only_the_last_byte_lacks_the_top_bit_and_it_is_never_a_wasted_zero()
    {
        for (var value = 1; value <= ushort.MaxValue; value++)
        {
            var bytes = Write((ushort)value);

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

        writer.PutVarUShort(16_384);

        Assert.Equal(sizeof(uint) + 3, writer.Length);
    }

    // A longer form than needed still means the same number; only past 16 bits is it refused
    [Theory]
    [InlineData((ushort)0, new byte[] { 0x80, 0x00 })]
    [InlineData((ushort)1, new byte[] { 0x81, 0x80, 0x00 })]
    [InlineData((ushort)127, new byte[] { 0xFF, 0x80, 0x00 })]
    public void A_longer_form_than_needed_still_reads(ushort value, byte[] encoded)
    {
        var reader = Reader(encoded);

        Assert.Equal(value, reader.GetVarUShort());
        Assert.True(reader.EndOfData);
    }

    // A third byte with the top bit set asks for a fourth, whether or not one is there
    [Theory]
    [InlineData((byte)0x80, true)]
    [InlineData((byte)0x83, true)]
    [InlineData((byte)0xFF, true)]
    [InlineData((byte)0x80, false)]
    public void A_fourth_byte_is_refused(byte third, bool fourthPresent)
    {
        var reader = fourthPresent ? Reader(0x80, 0x80, third, 0x00) : Reader(0x80, 0x80, third);

        var error = Assert.Throws<InvalidDataException>(() => reader.GetVarUShort());
        Assert.Contains("more than 3 bytes", error.Message);
    }

    [Theory]
    [InlineData((byte)0x04)]
    [InlineData((byte)0x40)]
    [InlineData((byte)0x7F)]
    public void A_third_byte_past_16_bits_is_refused(byte third)
    {
        var reader = Reader(0xFF, 0xFF, third, 0x00);

        var error = Assert.Throws<InvalidDataException>(() => reader.GetVarUShort());
        Assert.Contains("16 bits", error.Message);
    }

    // What a VarUInt writes for a value a ushort cannot hold is refused, rather than read as a smaller number
    [Theory]
    [InlineData(65_536u)]
    [InlineData(100_000u)]
    [InlineData(2_097_151u)]
    public void A_VarUInt_past_ushort_range_is_refused(uint value)
    {
        var writer = new NetDataWriter();
        writer.PutVarUInt(value);

        Assert.Throws<InvalidDataException>(() => Reader(writer.CopyData()).GetVarUShort());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void A_stream_that_ends_mid_varint_is_refused(int length)
    {
        var reader = Reader(Write(ushort.MaxValue)[..length]);

        var error = Assert.Throws<InvalidDataException>(() => reader.GetVarUShort());
        Assert.Contains("ends", error.Message);
    }

    [Fact]
    public void An_empty_stream_is_refused()
    {
        Assert.Throws<InvalidDataException>(() => Reader().GetVarUShort());
    }

    [Theory]
    [MemberData(nameof(Encodings))]
    public void As_a_serializable_it_writes_and_reads_the_same_bytes(ushort value, byte[] encoded)
    {
        var writer = new NetDataWriter();
        writer.Put(new VarUShort(value));

        Assert.Equal(encoded, writer.CopyData());

        var reader = Reader(encoded);
        Assert.Equal(value, reader.Get<VarUShort>().Value);
        Assert.True(reader.EndOfData);
    }

    [Fact]
    public void It_converts_to_and_from_ushort()
    {
        VarUShort fromUShort = (ushort)16_384;
        ushort toUShort = new VarUShort(300);

        Assert.Equal((ushort)16_384, fromUShort.Value);
        Assert.Equal((ushort)300, toUShort);
        Assert.Equal((ushort)0, default(VarUShort).Value);
    }

    [Fact]
    public void Equal_values_are_equal_and_hash_alike()
    {
        var a = new VarUShort(300);
        var b = new VarUShort(300);
        var c = new VarUShort(301);

        Assert.True(a == b);
        Assert.False(a != b);
        Assert.True(a.Equals(b));
        Assert.True(a.Equals((object)b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());

        Assert.False(a == c);
        Assert.True(a != c);
        Assert.False(a.Equals((object)300));
    }

    // So an id that holds one prints its number
    [Fact]
    public void It_prints_as_its_value()
    {
        Assert.Equal("16384", new VarUShort(16_384).ToString());
        Assert.Equal("PlayerId[7]", $"PlayerId[{new VarUShort(7)}]");
    }

    private static NetDataReader Reader(params byte[] bytes)
        => new(bytes);

    private static byte[] Write(ushort value)
    {
        var writer = new NetDataWriter();
        writer.PutVarUShort(value);
        return writer.CopyData();
    }

    private static void AssertRoundTrips(ushort value)
    {
        var bytes = Write(value);
        Assert.Equal(VarUShort.SizeOf(value), bytes.Length);

        var reader = new NetDataReader(bytes);
        Assert.Equal(value, reader.GetVarUShort());
        Assert.True(reader.EndOfData);
    }
}
