namespace Yooni.Native.Container;

public struct NativeStringHash64 : IHashFunction<NativeString64>
{
    public uint ComputeHash(in NativeString64 value)
        => (uint)value.GetHashCode();
}