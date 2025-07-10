using System;
using System.Collections.Generic;

namespace ReadyM.Api.Multiplayer;

public sealed class NetworkedComponentRegistry : INetworkedComponentRegistry
{
    private readonly List<Action<INetworkedComponentRegistryCallback>> _acceptCallbacks = new();

    public NetworkedComponentRegistry(IEnumerable<INetworkedComponentRegistration> registrations)
    {
        foreach (var registration in registrations)
        {
            registration.Register(this);
        }
    }
    
    public INetworkedComponentRegistry RegisterComponent<T>()
        where T : struct, INetworkedComponent
    {
        _acceptCallbacks.Add(callback =>
        {
            callback.AcceptNetworkedComponent<T>();
        });
        
        return this;
    }

    public void Accept(INetworkedComponentRegistryCallback callback)
    {
        foreach (var acceptCallbacks in _acceptCallbacks)
        {
            acceptCallbacks(callback);
        }
    }
}
