using System;
using System.Collections.Generic;
using LiteNetLib;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

public class NetworkedComponentRegistry(IEnumerable<INetworkedComponentRegistration> registrations)
    : ComponentRegistryBase<INetworkedComponentRegistry, INetworkedComponent>(registrations), INetworkedComponentRegistry
{
    private byte _nextComponentId;
    private readonly Dictionary<Type, (NetworkedComponentId Id, DeliveryMethod DeliveryMethod)> _ids = new();

    public override INetworkedComponentRegistry RegisterComponent<T>(T defaultValue = default)
    {
        var id = new NetworkedComponentId(_nextComponentId++);
        _ids.Add(typeof(T), (id, DeliveryMethod.Unreliable));
        return base.RegisterComponent(defaultValue);
    }

    public INetworkedComponentRegistry RegisterComponent<T>(DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable, T defaultValue = default)
        where T : struct, INetworkedComponent
    {
        var id = new NetworkedComponentId(_nextComponentId++);
        _ids.Add(typeof(T), (id, deliveryMethod));
        return base.RegisterComponent(defaultValue);
    }

    public NetworkedComponentId GetNetworkedComponentId<T>()
        => _ids[typeof(T)].Id;

    public DeliveryMethod GetNetworkedComponentDeliveryMethod<T>()
        => _ids[typeof(T)].DeliveryMethod;
}