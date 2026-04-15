namespace Yooni.Native.Container;

public unsafe struct MemoryHash<T> : IHashFunction<T>
    where T : unmanaged
{
    public uint ComputeHash(in T value)
    {
        fixed (T* ptr = &value)
        {
            return ByteHashUtils.GetByteHash((byte*)ptr, sizeof(int));
        }
    }
}