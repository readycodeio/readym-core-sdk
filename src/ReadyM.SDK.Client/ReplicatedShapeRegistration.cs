using System.ComponentModel;
using LiteNetLib;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.ECS.Registry;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Client;

/// Tells the client which components arrive over the wire, taken from the shapes mods declared.
/// A game registers this after its own components, so the ids line up with the server, which
/// numbers everything a mod declared after everything it registered itself.
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class ReplicatedShapeRegistration : INetworkedComponentRegistration
{
    // Explicit, because the registry type is internal to the multiplayer API and a public method
    // cannot name it. A game registers this type; only the host ever calls it.
    void IComponentRegistrationBase<INetworkedComponentRegistry, INetworkedComponent>.Register(
        INetworkedComponentRegistry registry)
    {
        foreach (var replicated in ReplicationRegistry.InWireOrder())
            registry.RegisterComponent(replicated.Component, Wire(replicated.Delivery));
    }

    // Reliable means ordered as well, the same reading the relay gives it.
    private static DeliveryMethod Wire(Delivery delivery) => delivery switch
    {
        Delivery.Unreliable => DeliveryMethod.Unreliable,
        _ => DeliveryMethod.ReliableOrdered
    };
}
