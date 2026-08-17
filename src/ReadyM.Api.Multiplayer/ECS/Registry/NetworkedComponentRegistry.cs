using System;
using System.Collections.Generic;
using DryIoc;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal class NetworkedComponentRegistry(IEnumerable<INetworkedComponentRegistration> registrations, ILogger logger)
    : ComponentRegistryBase<INetworkedComponentRegistry, INetworkedComponent>(registrations), INetworkedComponentRegistry
{
    protected readonly Dictionary<string, (NetworkedComponentId Id, DeliveryMethod DeliveryMethod)> componentIds = new();
    protected readonly Dictionary<NetworkedComponentId, Type> componentTypes = new();

    public INetworkedComponentRegistry RegisterComponent<T>(DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable, T defaultValue = default)
        where T : struct, INetworkedComponent
    {
        var id = new NetworkedComponentId(GetNextComponentId());
        componentIds.Add(typeof(T).FullName!, (id, deliveryMethod));
        componentTypes.Add(id, typeof(T));

        logger.LogInformation("[NetComp] Registered networked component {Id}: {ComponentType} ({ComponentFullName}) delivery {DeliveryMethod}", id, typeof(T).Name, typeof(T).FullName, deliveryMethod);
        return base.RegisterComponent(defaultValue);
    }

    public NetworkedComponentId GetNetworkedComponentId(Type type)
        => componentIds[type.FullName!].Id;

    public NetworkedComponentId GetNetworkedComponentId<T>()
        => componentIds[typeof(T).FullName!].Id;

    public NetworkedComponentId GetNetworkedComponentId(string typeFullName)
        => componentIds[typeFullName].Id;

    public Type GetComponentType(NetworkedComponentId componentId)
        => componentTypes[componentId];

    public DeliveryMethod GetNetworkedComponentDeliveryMethod<T>()
        => componentIds[typeof(T).FullName!].DeliveryMethod;
}