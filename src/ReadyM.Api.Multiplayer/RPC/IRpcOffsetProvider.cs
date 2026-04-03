namespace ReadyM.Api.Multiplayer.RPC;

internal sealed class RpcOffsetProvider
{
    public byte CurrentOffset { get; private set; }

    public byte GetNextOffset(byte events)
    {
        var offset = CurrentOffset;
        CurrentOffset += events;
        return offset;
    }
}