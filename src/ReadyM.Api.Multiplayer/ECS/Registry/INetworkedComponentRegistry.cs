using System;
using LiteNetLib;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal interface INetworkedComponentRegistry : IComponentRegistryBase<INetworkedComponentRegistry, INetworkedComponent>
{
    INetworkedComponentRegistry RegisterComponent(Type componentTyp, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableUnordered);
    INetworkedComponentRegistry RegisterComponent<T>(DeliveryMethod deliveryMethod = DeliveryMethod.ReliableUnordered)
        where T : struct, INetworkedComponent;

    NetworkedComponentId GetNetworkedComponentId(Type type);
    NetworkedComponentId GetNetworkedComponentId<T>();
    NetworkedComponentId GetNetworkedComponentId(string typeFullName);
    Type GetComponentType(NetworkedComponentId componentId);
    DeliveryMethod GetNetworkedComponentDeliveryMethod<T>();
}
