using System;
using System.Collections.Generic;
using System.Text;
using LiteNetLib.Utils;
using NUnit.Framework;
using Yooni.Native.Container;

namespace Yooni.Native.Serialization.Tests;

[TestFixture]
public unsafe class NativeStringNetDataStressTests
{
    private enum NativeStringKind : byte
    {
        String64 = 1,
        String256 = 2,
    }

    private sealed record ExpectedEntry(
        NativeStringKind Kind,
        bool IsWide,
        string ManagedValue,
        byte[] RawBytes);

    [Test, Category("String"), Category("ECS"), Category("Stress")]
    public void StressTest_NetDataWriter_RoundTrips_MostlyNativeString64And256()
    {
        var rng = new Random(123456);
        var writer = new NetDataWriter();
        var expected = new List<ExpectedEntry>(capacity: 12000);

        // Large enough to force a substantial buffer and a lot of sequential reads.
        const int itemCount = 12000;

        for (var i = 0; i < itemCount; i++)
        {
            // Bias heavily toward NativeString64/256, with 64 appearing a bit more often.
            var roll = rng.Next(100);
            var kind = roll < 70
                ? NativeStringKind.String64
                : NativeStringKind.String256;

            var isWide = rng.Next(2) == 0;
            var capacity = kind == NativeStringKind.String64
                ? NativeString64.Capacity
                : NativeString256.Capacity;
            if (isWide)
                capacity /= 2;

            var managed = CreateRandomStringFittingCapacity(rng, capacity, isWide);
            var rawBytes = Encode(managed, isWide);

            Assert.That(rawBytes.Length, Is.LessThanOrEqualTo(capacity), "Generated string exceeded capacity");

            // Construct the actual native string instance first, so we stress the real constructors.
            if (kind == NativeStringKind.String64)
            {
                var native = new NativeString64(managed, isWide);
                Assert.That(native.ToManaged(), Is.EqualTo(managed));
                Assert.That(native.Length, Is.EqualTo(rawBytes.Length));
                WriteNativeString64(writer, native);
            }
            else
            {
                var native = new NativeString256(managed, isWide);
                Assert.That(native.ToManaged(), Is.EqualTo(managed));
                Assert.That(native.Length, Is.EqualTo(rawBytes.Length));
                WriteNativeString256(writer, native);
            }

            expected.Add(new ExpectedEntry(kind, isWide, managed, rawBytes));
        }

        var reader = new NetDataReader(writer.CopyData());

        for (var i = 0; i < expected.Count; i++)
        {
            var entry = expected[i];
            var kind = (NativeStringKind)reader.GetByte();
            var isWide = reader.GetBool();
            var length = reader.GetUShort();

            Assert.That(kind, Is.EqualTo(entry.Kind), $"Kind mismatch at index {i}");
            Assert.That(isWide, Is.EqualTo(entry.IsWide), $"IsWide mismatch at index {i}");
            Assert.That(length, Is.EqualTo(entry.RawBytes.Length), $"Length mismatch at index {i}");

            var rawBytes = new byte[length];
            reader.GetBytes(rawBytes, length);

            CollectionAssert.AreEqual(entry.RawBytes, rawBytes, $"Serialized bytes mismatch at index {i}");

            if (kind == NativeStringKind.String64)
            {
                var native = new NativeString64(rawBytes, rawBytes.Length, isWide);

                Assert.That(native.IsWide, Is.EqualTo(entry.IsWide), $"NativeString64 IsWide mismatch at index {i}");
                Assert.That(native.Length, Is.EqualTo(entry.RawBytes.Length), $"NativeString64 Length mismatch at index {i}");
                Assert.That(native.ToManaged(), Is.EqualTo(entry.ManagedValue), $"NativeString64 managed value mismatch at index {i}");

                var copied = new byte[native.Length];
                fixed (byte* dest = copied)
                {
                    native.CopyTo(dest);
                }

                CollectionAssert.AreEqual(entry.RawBytes, copied, $"NativeString64 CopyTo mismatch at index {i}");
            }
            else
            {
                var native = new NativeString256(rawBytes, rawBytes.Length, isWide);

                Assert.That(native.IsWide, Is.EqualTo(entry.IsWide), $"NativeString256 IsWide mismatch at index {i}");
                Assert.That(native.Length, Is.EqualTo(entry.RawBytes.Length), $"NativeString256 Length mismatch at index {i}");
                Assert.That(native.ToManaged(), Is.EqualTo(entry.ManagedValue), $"NativeString256 managed value mismatch at index {i}");

                var copied = new byte[native.Length];
                fixed (byte* dest = copied)
                {
                    native.CopyTo(dest);
                }

                CollectionAssert.AreEqual(entry.RawBytes, copied, $"NativeString256 CopyTo mismatch at index {i}");
            }
        }

        Assert.That(reader.AvailableBytes, Is.EqualTo(0), "Reader should be fully consumed");
    }

    private static void WriteNativeString64(NetDataWriter writer, NativeString64 value)
    {
        writer.Put((byte)NativeStringKind.String64);
        writer.Put(value.IsWide);
        writer.Put((ushort)value.Length);

        var bytes = new byte[value.Length];
        fixed (byte* dest = bytes)
        {
            value.CopyTo(dest);
        }

        writer.Put(bytes);
    }

    private static void WriteNativeString256(NetDataWriter writer, NativeString256 value)
    {
        writer.Put((byte)NativeStringKind.String256);
        writer.Put(value.IsWide);
        writer.Put((ushort)value.Length);

        var bytes = new byte[value.Length];
        fixed (byte* dest = bytes)
        {
            value.CopyTo(dest);
        }

        writer.Put(bytes);
    }

    private static byte[] Encode(string value, bool isWide)
        => isWide ? Encoding.Unicode.GetBytes(value) : Encoding.UTF8.GetBytes(value);

    private static string CreateRandomStringFittingCapacity(Random rng, int capacity, bool isWide)
    {
        // ASCII-only payload keeps byte sizing deterministic:
        // UTF-8: 1 byte/char
        // UTF-16LE (Encoding.Unicode): 2 bytes/char
        //
        // Bias toward longer strings so the buffer fills faster and exercises boundaries better.
        int maxChars = isWide ? capacity / 2 : capacity;
        if (maxChars <= 0)
            return string.Empty;

        int lengthRoll = rng.Next(100);
        int charCount;

        if (lengthRoll < 10)
        {
            charCount = 0;
        }
        else if (lengthRoll < 25)
        {
            charCount = rng.Next(1, Math.Min(8, maxChars) + 1);
        }
        else if (lengthRoll < 75)
        {
            var min = Math.Max(1, maxChars / 2);
            charCount = rng.Next(min, maxChars + 1);
        }
        else
        {
            // Frequently hit exact-capacity / near-capacity values.
            charCount = Math.Max(1, maxChars - rng.Next(Math.Min(4, maxChars)));
        }

        var chars = new char[charCount];
        for (var i = 0; i < chars.Length; i++)
        {
            const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-./:";
            chars[i] = alphabet[rng.Next(alphabet.Length)];
        }

        var result = new string(chars);
        var encoded = Encode(result, isWide);

        Assert.That(encoded.Length, Is.LessThanOrEqualTo(capacity));
        return result;
    }
}