using ReadyM.Api.Multiplayer.ConflictResolution;

namespace ReadyM.Relay.Client.ConflictResolution;

internal interface IClientNetworkTime : INetworkTime
{
    void SetObservedTime(uint serverTime);
}