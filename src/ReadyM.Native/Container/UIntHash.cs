namespace ReadyM.Native.Container;

public struct UIntHash : IHashFunction<uint>
{
    public uint ComputeHash(in uint value)
        => value;
}