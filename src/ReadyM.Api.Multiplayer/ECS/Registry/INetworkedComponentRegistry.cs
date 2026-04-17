using System;
using LiteNetLib;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Interop;

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
    void RunQuery(NetworkedComponentId componentId, EmbedQueryDelegate callbackPtr);
    DeliveryMethod GetNetworkedComponentDeliveryMethod<T>();
}