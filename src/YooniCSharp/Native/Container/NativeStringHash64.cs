namespace Yooni.Native.Container;

public unsafe struct NativeStringHash64 : IHashFunction<NativeString64>
{
    public uint ComputeHash(in NativeString64 value)
        => ByteHashUtils.GetByteHash((byte*)value.GetChars(), value.Length);
}