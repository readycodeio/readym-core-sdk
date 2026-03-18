using System;

namespace ReadyM.Relay.Client;

public interface IScopedLifetime : IDisposable
{
    void OnScopeStart();
}