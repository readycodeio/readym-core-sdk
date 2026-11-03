using System;
using NUnit.Framework;
using ReadyM.Native.Container;

namespace ReadyM.Native.Tests.Container;

[TestFixture]
public unsafe class NativeString64ConstructorTests : NativeStringConstructorTests<NativeString64>
{
    protected override int Capacity => NativeString64.Capacity;

    protected override NativeString64 FromPointer(byte* bytes, int length, bool isWide)
        => new(bytes, length, isWide);

    protected override NativeString64 FromBytes(byte[] bytes, int length, bool isWide)
        => new(bytes, length, isWide);

    protected override NativeString64 FromBytes(byte[] bytes, int offset, int length, bool isWide)
        => new(bytes, offset, length, isWide);

    protected override NativeString64 FromString(string? value, bool isWide)
        => new(value, isWide);
}