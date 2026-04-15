namespace Yooni.Native.Container;

public unsafe struct NativeStringHash256 : IHashFunction<NativeString256>
{
    public uint ComputeHash(in NativeString256 value)
        => ByteHashUtils.GetByteHash((byte*)value.GetChars(), value.Length);
}