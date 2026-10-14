using System;
using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal interface INetworkedComponentRegistry : IComponentRegistryBase<INetworkedComponentRegistry, INetworkedComponent>
{
    INetworkedComponentRegistry RegisterComponent(Type componentTyp, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered);
    INetworkedComponentRegistry RegisterComponent<T>(DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        where T : struct, INetworkedComponent;
    INetworkedComponentRegistry RegisterModComponent(ModComponentInfo info, string typeFullName, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered);

    NetworkedComponentId GetNetworkedComponentId(Type type);
    NetworkedComponentId GetNetworkedComponentId<T>();
    NetworkedComponentId GetNetworkedComponentId(string typeFullName);
    Type GetComponentType(NetworkedComponentId componentId);
    DeliveryMethod GetNetworkedComponentDeliveryMethod<T>();
    DeliveryMethod GetNetworkedComponentDeliveryMethod(string typeFullName);
}
