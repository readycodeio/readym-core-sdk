namespace Yooni.Native.Container;

public struct IntHash : IHashFunction<int>
{
    public uint ComputeHash(int value)
    {
        unchecked
        {
            return (uint)value;
        }
    }
}