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
    NetworkedComponentId GetNetworkedComponentId(string typeFullName);
    Type GetComponentType(NetworkedComponentId componentId);
    DeliveryMethod GetNetworkedComponentDeliveryMethod<T>();

    void RunQuery(NetworkedComponentId componentId, EmbedQueryDelegate1 callbackPtr);
    void RunQuery(NetworkedComponentId c1, NetworkedComponentId c2, EmbedQueryDelegate2 callbackPtr);
}