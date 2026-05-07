namespace Yooni.Native.Container;

public struct NativeStringHash256 : IHashFunction<NativeString256>
{
    public uint ComputeHash(in NativeString256 value)
        => (uint)value.GetHashCode();
}