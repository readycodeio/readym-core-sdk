using System;
using LiteNetLib;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal interface INetworkedComponentRegistry : IComponentRegistryBase<INetworkedComponentRegistry, INetworkedComponent>
{
    INetworkedComponentRegistry RegisterComponent<T>(T defaultValue = default)
        where T : struct, INetworkedComponent;
    INetworkedComponentRegistry RegisterComponent<T>(DeliveryMethod deliveryMethod, T defaultValue = default)
        where T : struct, INetworkedComponent;

    NetworkedComponentId GetNetworkedComponentId(Type type);
    NetworkedComponentId GetNetworkedComponentId<T>();
    Type GetComponentType(NetworkedComponentId componentId);
    DeliveryMethod GetNetworkedComponentDeliveryMethod<T>();
}