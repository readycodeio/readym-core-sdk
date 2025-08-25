using System;
using System.Collections.Generic;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

public class NetworkedComponentRegistry(IEnumerable<INetworkedComponentRegistration> registrations)
    : ComponentRegistryBase<INetworkedComponentRegistry, INetworkedComponent>(registrations), INetworkedComponentRegistry
{
    private byte _nextComponentId;
    private readonly Dictionary<Type, NetworkedComponentId> _ids = new();

    public override INetworkedComponentRegistry RegisterComponent<T>(T defaultValue = default)
    {
        var id = new NetworkedComponentId(_nextComponentId++);
        _ids.Add(typeof(T), id);
        return base.RegisterComponent(defaultValue);
    }

    public NetworkedComponentId GetNetworkedComponentId<T>()
        => _ids[typeof(T)];
}