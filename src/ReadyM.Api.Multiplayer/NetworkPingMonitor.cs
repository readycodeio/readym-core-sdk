using System;
using ReadyM.Relay.Client;

namespace ReadyM.Api.Multiplayer;

public class NetworkPingMonitor : IDisposable
{
    private readonly IRelayClient _relayClient;
    public event Action<int>? OnPingUpdated;
    
    public int CurrentPing { get; private set; }

    public NetworkPingMonitor(IRelayClient relayClient)
    {
        _relayClient = relayClient;
        relayClient.OnPingUpdated += HandlePingUpdated;
    }

    public void Dispose()
    {
        _relayClient.OnPingUpdated -= HandlePingUpdated;
    }

    private void HandlePingUpdated(int ping)
    {
        CurrentPing = ping;
        OnPingUpdated?.Invoke(ping);
    }
}