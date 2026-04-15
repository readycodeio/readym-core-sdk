namespace Yooni.Native.Container;

public struct IntHash : IHashFunction<int>
{
    public uint ComputeHash(in int value)
    {
        unchecked
        {
            return (uint)value;
        }
    }
}