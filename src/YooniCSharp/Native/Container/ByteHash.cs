namespace Yooni.Native.Container;

public struct ByteHash : IHashFunction<byte>
{
    public uint ComputeHash(in byte value)
        => value;
}