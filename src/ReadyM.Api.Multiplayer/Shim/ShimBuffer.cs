namespace ReadyM.Api.Multiplayer.Shim;

internal struct ShimBuffer
{
    public byte[]? Data { get; }

    public int Offset { get; }
    public int MaxSize { get; }
    
    public ShimBuffer(byte[] data, int offset, int? length = null)
    {
        Data = data;
        Offset = offset;
        MaxSize = length ?? data.Length - offset;
    }
    
    public ShimBuffer(byte[] data)
        : this(data, 0, null)
    {
        // empty
    }
}
