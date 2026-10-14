using System.Text;
using LiteNetLib.Utils;
using Xunit;
using Yooni.Native.Container;
using Yooni.Native.Serialization;

namespace ReadyM.Api.Tests;

// A byte length, a form byte and the bytes. A wide string of ASCII characters goes one byte a character
public class NativeStringSerializationTests
{
    private const byte Narrow = 0;
    private const byte Wide = 1;
    private const byte WideAsAscii = 2;

    public static TheoryData<string, bool, byte[]> Encodings => new()
    {
        { "abc", false, [0x03, Narrow, 0x61, 0x62, 0x63] },
        { "abc", true, [0x03, WideAsAscii, 0x61, 0x62, 0x63] },
        { "żółw", true, [0x08, Wide, 0x7C, 0x01, 0xF3, 0x00, 0x42, 0x01, 0x77, 0x00] },
        { "żółw", false, [0x07, Narrow, 0xC5, 0xBC, 0xC3, 0xB3, 0xC5, 0x82, 0x77] },
        { "", false, [0x00, Narrow] },
        { "", true, [0x00, WideAsAscii] },
        // Low byte past ASCII, then high byte set: either keeps the whole string wide
        { "é", true, [0x02, Wide, 0xE9, 0x00] },
        { "Ā", true, [0x02, Wide, 0x00, 0x01] },
        { "aé", true, [0x04, Wide, 0x61, 0x00, 0xE9, 0x00] },
        { "\u007F", true, [0x01, WideAsAscii, 0x7F] },
    };

    [Theory]
    [MemberData(nameof(Encodings))]
    public void A_string_256_writes_exactly_these_bytes(string text, bool wide, byte[] expected)
    {
        Assert.Equal(expected, Write(new NativeString256(text, wide)));
    }

    [Theory]
    [MemberData(nameof(Encodings))]
    public void A_string_64_writes_exactly_these_bytes(string text, bool wide, byte[] expected)
    {
        Assert.Equal(expected, Write(new NativeString64(text, wide)));
    }

    [Theory]
    [MemberData(nameof(Encodings))]
    public void A_string_256_reads_back_with_its_bytes_and_width_and_stops_after_them(string text, bool wide, byte[] encoded)
    {
        var reader = Reader([.. encoded, 0xAB]);
        var value = new NativeString256("not read", !wide);

        value.Deserialize(reader);

        Assert.True(value == new NativeString256(text, wide));
        Assert.Equal(wide, value.IsWide);
        Assert.Equal(text, value.ToString());
        Assert.Equal(0xAB, reader.GetByte());
        Assert.True(reader.EndOfData);
    }

    [Theory]
    [MemberData(nameof(Encodings))]
    public void A_string_64_reads_back_with_its_bytes_and_width_and_stops_after_them(string text, bool wide, byte[] encoded)
    {
        var reader = Reader([.. encoded, 0xAB]);
        var value = new NativeString64("not read", !wide);

        value.Deserialize(reader);

        Assert.True(value == new NativeString64(text, wide));
        Assert.Equal(wide, value.IsWide);
        Assert.Equal(text, value.ToString());
        Assert.Equal(0xAB, reader.GetByte());
        Assert.True(reader.EndOfData);
    }

    [Fact]
    public void The_header_is_two_bytes()
    {
        Assert.Equal(2, Write(new NativeString256("", false)).Length);
        Assert.Equal(2 + 34, Write(new NativeString256("Actor.Locomotion.MoveMode.Standing", true)).Length);
    }

    [Theory]
    [InlineData(127, true)]
    [InlineData(254, false)]
    public void A_string_256_at_full_capacity_round_trips(int characters, bool wide)
    {
        AssertRoundTrips(new NativeString256(new string('x', characters), wide));
        AssertRoundTrips(new NativeString256(new string('ł', wide ? characters : characters / 2), wide));
    }

    [Theory]
    [InlineData(31, true)]
    [InlineData(62, false)]
    public void A_string_64_at_full_capacity_round_trips(int characters, bool wide)
    {
        AssertRoundTrips(new NativeString64(new string('x', characters), wide));
        AssertRoundTrips(new NativeString64(new string('ł', wide ? characters : characters / 2), wide));
    }

    [Fact]
    public void Random_strings_of_every_kind_round_trip()
    {
        var random = new Random(20261013);
        for (var i = 0; i < 2_000; i++)
        {
            foreach (var wide in new[] { false, true })
            foreach (var ascii in new[] { false, true })
            {
                AssertRoundTrips(new NativeString256(RandomText(random, ascii, wide, NativeString256.Capacity), wide));
                AssertRoundTrips(new NativeString64(RandomText(random, ascii, wide, NativeString64.Capacity), wide));
            }
        }
    }

    [Fact]
    public void An_unknown_form_is_refused()
    {
        AssertRefused256([0x03, 0x03, 0x61, 0x62, 0x63], "form");
        AssertRefused64([0x03, 0x03, 0x61, 0x62, 0x63], "form");
    }

    [Fact]
    public void A_length_past_capacity_is_refused()
    {
        AssertRefused256([255, Narrow, .. new byte[255]], "capacity");
        AssertRefused64([63, Narrow, .. new byte[63]], "capacity");
    }

    [Fact]
    public void An_ascii_length_that_widens_past_capacity_is_refused()
    {
        AssertRefused256([128, WideAsAscii, .. Ascii(128)], "capacity");
        AssertRefused64([32, WideAsAscii, .. Ascii(32)], "capacity");
    }

    [Fact]
    public void An_ascii_length_that_widens_to_exactly_capacity_reads()
    {
        var value = default(NativeString256);
        value.Deserialize(Reader([127, WideAsAscii, .. Ascii(127)]));

        Assert.Equal(254, value.Length);
        Assert.True(value.IsWide);
    }

    [Theory]
    [InlineData(new byte[] { 0x05, Narrow, 0x61, 0x62 })]
    [InlineData(new byte[] { 0x03, WideAsAscii, 0x61 })]
    [InlineData(new byte[] { 0x02 })]
    [InlineData(new byte[] { })]
    public void A_stream_that_ends_early_is_refused(byte[] encoded)
    {
        Assert.ThrowsAny<Exception>(() => Read256(encoded));
        Assert.ThrowsAny<Exception>(() => Read64(encoded));
    }

    // ---

    private static byte[] Write(in NativeString256 value)
    {
        var writer = new NetDataWriter();
        value.Serialize(writer);
        return writer.CopyData();
    }

    private static byte[] Write(in NativeString64 value)
    {
        var writer = new NetDataWriter();
        value.Serialize(writer);
        return writer.CopyData();
    }

    private static void AssertRoundTrips(NativeString256 sent)
    {
        var reader = new NetDataReader(Write(sent));
        var received = default(NativeString256);
        received.Deserialize(reader);

        Assert.True(received == sent, $"\"{sent}\" (wide: {sent.IsWide}) came back as \"{received}\" (wide: {received.IsWide})");
        Assert.True(reader.EndOfData);
    }

    private static void AssertRoundTrips(NativeString64 sent)
    {
        var reader = new NetDataReader(Write(sent));
        var received = default(NativeString64);
        received.Deserialize(reader);

        Assert.True(received == sent, $"\"{sent}\" (wide: {sent.IsWide}) came back as \"{received}\" (wide: {received.IsWide})");
        Assert.True(reader.EndOfData);
    }

    private static void AssertRefused256(byte[] encoded, string reason)
    {
        var error = Assert.Throws<InvalidOperationException>(() => Read256(encoded));
        Assert.Contains(reason, error.Message);
    }

    private static void AssertRefused64(byte[] encoded, string reason)
    {
        var error = Assert.Throws<InvalidOperationException>(() => Read64(encoded));
        Assert.Contains(reason, error.Message);
    }

    private static NetDataReader Reader(params byte[] bytes)
        => new(bytes);

    private static NativeString256 Read256(byte[] encoded)
    {
        var value = default(NativeString256);
        value.Deserialize(Reader(encoded));
        return value;
    }

    private static NativeString64 Read64(byte[] encoded)
    {
        var value = default(NativeString64);
        value.Deserialize(Reader(encoded));
        return value;
    }

    private static byte[] Ascii(int count)
        => Enumerable.Repeat((byte)'a', count).ToArray();

    // Text that fits: a wide string takes two bytes a character, a narrow one its UTF-8 length
    private static string RandomText(Random random, bool ascii, bool wide, int capacity)
    {
        var builder = new StringBuilder();
        var target = random.Next(0, wide ? capacity / 2 + 1 : capacity + 1);
        while (builder.Length < target)
        {
            var c = ascii ? (char)random.Next(0, 0x80) : (char)random.Next(0x80, 0x800);
            builder.Append(c);
            if ((wide ? builder.Length * 2 : Encoding.UTF8.GetByteCount(builder.ToString())) > capacity)
            {
                builder.Length--;
                break;
            }
        }

        return builder.ToString();
    }
}
