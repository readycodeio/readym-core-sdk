using System;
using LiteNetLib;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

// TODO: Not public
public interface INetworkedComponentRegistry : IComponentRegistryBase<INetworkedComponentRegistry, INetworkedComponent>
{
    INetworkedComponentRegistry RegisterComponent<T>(DeliveryMethod deliveryMethod = DeliveryMethod.ReliableUnordered, T defaultValue = default)
        where T : struct, INetworkedComponent;

    NetworkedComponentId GetNetworkedComponentId(Type type);
    NetworkedComponentId GetNetworkedComponentId<T>();
    NetworkedComponentId GetNetworkedComponentId(string typeFullName);
    Type GetComponentType(NetworkedComponentId componentId);
    DeliveryMethod GetNetworkedComponentDeliveryMethod<T>();
}