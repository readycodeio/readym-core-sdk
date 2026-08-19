using System;
using System.Collections.Generic;
using LiteNetLib;
using Microsoft.Extensions.Logging;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal class NetworkedComponentRegistry(ServerSideSettings serverSide, IEnumerable<INetworkedComponentRegistration> registrations, ILogger logger)
    : ComponentRegistryBase<INetworkedComponentRegistry, INetworkedComponent>(registrations), INetworkedComponentRegistry
{
    protected readonly ServerSideSettings ServerSide = serverSide;
    protected readonly ILogger Logger = logger;
    protected readonly Dictionary<string, (NetworkedComponentId Id, DeliveryMethod DeliveryMethod)> ComponentIds = new();
    protected readonly Dictionary<NetworkedComponentId, Type> ComponentTypes = new();

    public INetworkedComponentRegistry RegisterComponent<T>(DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable, T defaultValue = default)
        where T : struct, INetworkedComponent
    {
        var id = new NetworkedComponentId(GetNextComponentId());
        ComponentIds.Add(typeof(T).FullName!, (id, deliveryMethod));
        ComponentTypes.Add(id, typeof(T));

        logger.LogInformation("[NetComp] Registered networked component {Id}: {ComponentType} ({ComponentFullName}) delivery {DeliveryMethod}", id, typeof(T).Name, typeof(T).FullName, deliveryMethod);
        return base.RegisterComponent(defaultValue);
    }

    public NetworkedComponentId GetNetworkedComponentId(Type type)
        => ComponentIds[type.FullName!].Id;

    public NetworkedComponentId GetNetworkedComponentId<T>()
        => ComponentIds[typeof(T).FullName!].Id;

    public NetworkedComponentId GetNetworkedComponentId(string typeFullName)
        => ComponentIds[typeFullName].Id;

    public Type GetComponentType(NetworkedComponentId componentId)
        => ComponentTypes[componentId];

    public DeliveryMethod GetNetworkedComponentDeliveryMethod<T>()
        => ComponentIds[typeof(T).FullName!].DeliveryMethod;
}
