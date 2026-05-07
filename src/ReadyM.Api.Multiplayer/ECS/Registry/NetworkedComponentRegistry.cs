using System;
using System.Collections.Generic;
using LiteNetLib;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal class NetworkedComponentRegistry(IEnumerable<INetworkedComponentRegistration> registrations)
    : ComponentRegistryBase<INetworkedComponentRegistry, INetworkedComponent>(registrations), INetworkedComponentRegistry
{
    private byte _nextComponentId;
    private readonly Dictionary<Type, (NetworkedComponentId Id, DeliveryMethod DeliveryMethod)> _componentIds = new();
    private readonly Dictionary<NetworkedComponentId, Type> _componentTypes = new();

    public new INetworkedComponentRegistry RegisterComponent<T>(T defaultValue = default)
        where T : struct, INetworkedComponent
        => RegisterComponent(DeliveryMethod.Unreliable, defaultValue);
    
    public INetworkedComponentRegistry RegisterComponent<T>(DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable, T defaultValue = default)
        where T : struct, INetworkedComponent
    {
        var id = new NetworkedComponentId(_nextComponentId++);
        _componentIds.Add(typeof(T), (id, deliveryMethod));
        _componentTypes.Add(id, typeof(T));
        return base.RegisterComponent(defaultValue);
    }

    public NetworkedComponentId GetNetworkedComponentId(Type type)
        => _ids[type].Id;

    public NetworkedComponentId GetNetworkedComponentId<T>()
        => _componentIds[typeof(T)].Id;

    public Type GetComponentType(NetworkedComponentId componentId)
        => _componentTypes[componentId];

    public DeliveryMethod GetNetworkedComponentDeliveryMethod<T>()
        => _componentIds[typeof(T)].DeliveryMethod;
}