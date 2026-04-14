namespace Yooni.Native.Container;

public struct ByteHash : IHashFunction<byte>
{
    public uint ComputeHash(byte value)
    {
        unchecked
        {
            return (uint)value;
        }
    }
}