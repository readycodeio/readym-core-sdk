using System;
using System.Collections.Generic;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal class NetworkedComponentRegistry(IEnumerable<INetworkedComponentRegistration> registrations, ILogger logger)
    : IdComponentRegistryBase<INetworkedComponentRegistry, INetworkedComponent>(registrations), INetworkedComponentRegistry
{
    protected readonly ILogger Logger = logger;
    protected readonly Dictionary<string, (NetworkedComponentId Id, DeliveryMethod DeliveryMethod)> ComponentIds = new();
    protected readonly Dictionary<NetworkedComponentId, Type> NetComponentTypes = new();

    public INetworkedComponentRegistry RegisterComponent(Type componentType, DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable)
    {
        if (!typeof(INetworkedComponent).IsAssignableFrom(componentType))
            throw new ArgumentException($"Type {componentType.FullName} does not implement INetworkedComponent", nameof(componentType));

        var id = new NetworkedComponentId(GetNextComponentId());
        ComponentIds.Add(componentType.FullName!, (id, deliveryMethod));
        NetComponentTypes.Add(id, componentType);

        Logger.LogInformation("[NetComp] Registered networked component {Id}: {ComponentType} ({ComponentFullName}) delivery {DeliveryMethod}", id, componentType.Name, componentType.FullName, deliveryMethod);
        return base.RegisterComponentImpl(componentType);
    }

    public INetworkedComponentRegistry RegisterComponent<T>(DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable)
        where T : struct, INetworkedComponent
    {
        var id = new NetworkedComponentId(GetNextComponentId());
        ComponentIds.Add(typeof(T).FullName!, (id, deliveryMethod));
        NetComponentTypes.Add(id, typeof(T));

        Logger.LogInformation("[NetComp] Registered networked component {Id}: {ComponentType} ({ComponentFullName}) delivery {DeliveryMethod}", id, typeof(T).Name, typeof(T).FullName, deliveryMethod);
        return base.RegisterComponentImpl(default(T));
    }

    public NetworkedComponentId GetNetworkedComponentId(Type type)
        => ComponentIds[type.FullName!].Id;

    public NetworkedComponentId GetNetworkedComponentId<T>()
        => ComponentIds[typeof(T).FullName!].Id;

    public NetworkedComponentId GetNetworkedComponentId(string typeFullName)
        => ComponentIds[typeFullName].Id;

    public Type GetComponentType(NetworkedComponentId componentId)
        => NetComponentTypes[componentId];

    public DeliveryMethod GetNetworkedComponentDeliveryMethod<T>()
        => ComponentIds[typeof(T).FullName!].DeliveryMethod;
}
