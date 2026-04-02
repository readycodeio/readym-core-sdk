using System;
using ReadyM.Api.DI;

namespace ReadyM.Api.Multiplayer.Client;

internal class NetworkPingMonitor(IRelayClient relayClient) : IHostedService
{
    public event Action<int>? OnPingUpdated;

    public int CurrentPing { get; private set; }

    public void OnScopeStart()
    {
        relayClient.OnPingUpdated += HandlePingUpdated;
    }

    public void Dispose()
    {
        relayClient.OnPingUpdated -= HandlePingUpdated;
    }

    private void HandlePingUpdated(int ping)
    {
        CurrentPing = ping;
        OnPingUpdated?.Invoke(ping);
    }
}