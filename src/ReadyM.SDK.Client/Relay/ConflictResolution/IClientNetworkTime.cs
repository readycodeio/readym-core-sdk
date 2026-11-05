using ReadyM.Api.Multiplayer.ConflictResolution;

namespace ReadyM.SDK.Client.Relay.ConflictResolution;

internal interface IClientNetworkTime : INetworkTime
{
    void SetObservedTime(uint serverTime);
}