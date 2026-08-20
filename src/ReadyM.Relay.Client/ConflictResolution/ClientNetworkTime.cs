namespace ReadyM.Relay.Client.ConflictResolution;

internal class ClientNetworkTime : IClientNetworkTime
{
    private uint _serverTime;

    public uint GetCurrentTime()
        => _serverTime;

    public void SetObservedTime(uint serverTime)
    {
        _serverTime = serverTime;
    }
}
