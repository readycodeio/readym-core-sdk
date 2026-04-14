namespace Yooni.Native.Container;

public unsafe struct MemoryHash<T> : IHashFunction<T>
    where T : unmanaged
{
    public uint ComputeHash(T value)
        => ByteHashUtils.GetByteHash((byte*)&value, sizeof(int));
}