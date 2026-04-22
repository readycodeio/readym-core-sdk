 using System;
using NUnit.Framework;

namespace Yooni.Native.Container.Tests;

[TestFixture]
public unsafe class NativeString256ConstructorTests : NativeStringConstructorTests<NativeString256>
{
    protected override int Capacity => NativeString256.Capacity;

    protected override NativeString256 FromPointer(byte* bytes, int length, bool isWide)
        => new(bytes, length, isWide);

    protected override NativeString256 FromBytes(byte[] bytes, int length, bool isWide)
        => new(bytes, length, isWide);

    protected override NativeString256 FromBytes(byte[] bytes, int offset, int length, bool isWide)
        => new(bytes, offset, length, isWide);

    protected override NativeString256 FromString(string? value, bool isWide)
        => new(value, isWide);
}