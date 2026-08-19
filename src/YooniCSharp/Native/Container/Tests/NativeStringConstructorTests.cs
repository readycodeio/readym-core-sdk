using System;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Yooni.Native.Container.Tests;

public abstract unsafe class NativeStringConstructorTests<TString>
    where TString : unmanaged, INativeString, IEquatable<TString>
{
    protected abstract int Capacity { get; }

    protected abstract TString FromPointer(byte* bytes, int length, bool isWide);
    protected abstract TString FromBytes(byte[] bytes, int length, bool isWide);
    protected abstract TString FromBytes(byte[] bytes, int offset, int length, bool isWide);
    protected abstract TString FromString(string? value, bool isWide);

    private static byte[] Encode(string value, bool isWide)
        => isWide ? Encoding.Unicode.GetBytes(value) : Encoding.UTF8.GetBytes(value);

    private string ExactFitString(bool isWide)
    {
        if (isWide)
        {
            Assert.That(Capacity % 2, Is.EqualTo(1).Or.EqualTo(0));
            return new string('a', Capacity / 2);
        }

        return new string('a', Capacity);
    }

    private string TooLongString(bool isWide)
    {
        if (isWide)
            return new string('a', (Capacity / 2) + 1);

        return new string('a', Capacity + 1);
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void PointerConstructor_CreatesString(bool isWide)
    {
        var bytes = Encode("test", isWide);

        fixed (byte* ptr = bytes)
        {
            var value = FromPointer(ptr, bytes.Length, isWide);

            Assert.AreEqual("test", value.ToManaged());
            Assert.AreEqual(bytes.Length, value.Length);
        }
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void PointerConstructor_ExactCapacity_Succeeds(bool isWide)
    {
        var str = ExactFitString(isWide);
        var bytes = Encode(str, isWide);
        Assert.That(bytes.Length, Is.EqualTo(Capacity));

        fixed (byte* ptr = bytes)
        {
            var value = FromPointer(ptr, bytes.Length, isWide);
            Assert.AreEqual(str, value.ToManaged());
            Assert.AreEqual(bytes.Length, value.Length);
        }
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void PointerConstructor_TooLong_Throws(bool isWide)
    {
        var str = TooLongString(isWide);
        var bytes = Encode(str, isWide);
        Assert.That(bytes.Length, Is.GreaterThan(Capacity));

        fixed (byte* ptr = bytes)
        {
            var p = ptr;
            Assert.Throws<InvalidOperationException>(() => FromPointer(p, bytes.Length, isWide));
        }
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void ByteArrayConstructor_CreatesString(bool isWide)
    {
        var bytes = Encode("test", isWide);

        var value = FromBytes(bytes, bytes.Length, isWide);

        Assert.AreEqual("test", value.ToManaged());
        Assert.AreEqual(bytes.Length, value.Length);
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void ByteArrayConstructor_ExactCapacity_Succeeds(bool isWide)
    {
        var str = ExactFitString(isWide);
        var bytes = Encode(str, isWide);
        Assert.That(bytes.Length, Is.EqualTo(Capacity));

        var value = FromBytes(bytes, bytes.Length, isWide);

        Assert.AreEqual(str, value.ToManaged());
        Assert.AreEqual(bytes.Length, value.Length);
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void ByteArrayConstructor_TooLong_Throws(bool isWide)
    {
        var str = TooLongString(isWide);
        var bytes = Encode(str, isWide);
        Assert.That(bytes.Length, Is.GreaterThan(Capacity));

        Assert.Throws<InvalidOperationException>(() => FromBytes(bytes, bytes.Length, isWide));
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void ByteArrayOffsetConstructor_CreatesString(bool isWide)
    {
        var payload = Encode("test", isWide);
        var bytes = new byte[payload.Length + 4];

        bytes[0] = 11;
        bytes[1] = 22;
        Buffer.BlockCopy(payload, 0, bytes, 2, payload.Length);
        bytes[^2] = 33;
        bytes[^1] = 44;

        var value = FromBytes(bytes, 2, payload.Length, isWide);

        Assert.AreEqual("test", value.ToManaged());
        Assert.AreEqual(payload.Length, value.Length);
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void ByteArrayOffsetConstructor_ExactCapacity_Succeeds(bool isWide)
    {
        var str = ExactFitString(isWide);
        var payload = Encode(str, isWide);
        var bytes = new byte[payload.Length + 6];

        Buffer.BlockCopy(payload, 0, bytes, 3, payload.Length);

        var value = FromBytes(bytes, 3, payload.Length, isWide);

        Assert.AreEqual(str, value.ToManaged());
        Assert.AreEqual(payload.Length, value.Length);
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void ByteArrayOffsetConstructor_TooLong_Throws(bool isWide)
    {
        var str = TooLongString(isWide);
        var payload = Encode(str, isWide);
        var bytes = new byte[payload.Length + 6];

        Buffer.BlockCopy(payload, 0, bytes, 3, payload.Length);

        Assert.Throws<InvalidOperationException>(() => FromBytes(bytes, 3, payload.Length, isWide));
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void StringConstructor_Null_CreatesEmpty(bool isWide)
    {
        var value = FromString(null, isWide);

        Assert.AreEqual(string.Empty, value.ToManaged());
        Assert.AreEqual(0, value.Length);
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void StringConstructor_ExactCapacity_Succeeds(bool isWide)
    {
        var str = ExactFitString(isWide);
        var bytes = Encode(str, isWide);
        Assert.That(bytes.Length, Is.EqualTo(Capacity));

        var value = FromString(str, isWide);

        Assert.AreEqual(str, value.ToManaged());
        Assert.AreEqual(bytes.Length, value.Length);
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void CopyTo_CopiesRawBytes(bool isWide)
    {
        const string str = "test";
        var expected = Encode(str, isWide);
        var value = FromString(str, isWide);

        var actual = new byte[value.Length];
        fixed (byte* dest = actual)
        {
            value.CopyTo(dest);
        }

        CollectionAssert.AreEqual(expected, actual);
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void CopyTo_ExactCapacity_CopiesRawBytes(bool isWide)
    {
        var str = ExactFitString(isWide);
        var expected = Encode(str, isWide);
        var value = FromString(str, isWide);

        var actual = new byte[value.Length];
        fixed (byte* dest = actual)
        {
            value.CopyTo(dest);
        }

        CollectionAssert.AreEqual(expected, actual);
    }
    
    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void StringConstructor_TooLong_ShouldThrow(bool isWide)
    {
        var str = isWide
            ? new string('a', (Capacity / 2) + 1)
            : new string('a', Capacity + 1);

        Assert.Throws<InvalidOperationException>(() => FromString(str, isWide));
    }
}