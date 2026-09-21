
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Attributes;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// What a mod's replicated shapes tell the host, which is the one thing the host needs before any
/// entity exists: which components go over the wire, and how.
public class ReplicationRegistryTests
{
    private static ReplicatedComponent? Find(Type component)
    {
        foreach (var registered in ReplicationRegistry.Snapshot())
            if (registered.Component == component)
                return registered;

        return null;
    }

    [Fact]
    public void A_replicated_shape_registers_its_component()
    {
        ReplicationRegistry.Clear();
        global::ReadyM.SDK.Generated.ShapeRegistrations.RegisterAll();

        Assert.Equal(Delivery.Reliable, Find(typeof(TelemetryComponent))?.Delivery);
    }

    [Fact]
    public void The_delivery_a_shape_asked_for_is_what_is_registered()
    {
        ReplicationRegistry.Clear();
        global::ReadyM.SDK.Generated.ShapeRegistrations.RegisterAll();

        Assert.Equal(Delivery.Unreliable, Find(typeof(MotionComponent))?.Delivery);
    }

    [Fact]
    public void A_local_shape_registers_nothing()
    {
        ReplicationRegistry.Clear();
        global::ReadyM.SDK.Generated.ShapeRegistrations.RegisterAll();

        Assert.Null(Find(typeof(ClimateComponent)));
    }

    /// The entry point a loader calls, which is what a target without module initializers relies on.
    [Fact]
    public void Registering_the_assembly_registers_every_replicated_shape()
    {
        ReplicationRegistry.Clear();

        Assert.Null(Find(typeof(TelemetryComponent)));

        global::ReadyM.SDK.Generated.ShapeRegistrations.RegisterAll();

        Assert.Equal(Delivery.Reliable, Find(typeof(TelemetryComponent))?.Delivery);
        Assert.Equal(Delivery.Unreliable, Find(typeof(MotionComponent))?.Delivery);
    }

    /// A reload registers the same component again rather than failing.
    [Fact]
    public void Registering_twice_keeps_one_entry()
    {
        ReplicationRegistry.Clear();

        global::ReadyM.SDK.Generated.ShapeRegistrations.RegisterAll();
        global::ReadyM.SDK.Generated.ShapeRegistrations.RegisterAll();

        var matching = 0;

        foreach (var registered in ReplicationRegistry.Snapshot())
            if (registered.Component == typeof(TelemetryComponent))
                matching++;

        Assert.Equal(1, matching);
    }

    /// A shape over a component that already replicates inherits that without asking. The delivery
    /// is this shape's default, not whatever the component's own owner registers it with.
    [Fact]
    public void A_shape_over_a_networked_component_registers_it()
    {
        ReplicationRegistry.Clear();
        global::ReadyM.SDK.Generated.ShapeRegistrations.RegisterAll();

        var perishable = Find(typeof(global::ReadyM.Api.Multiplayer.ECS.Components.EmptyScopeDeletionComponent));

        Assert.NotNull(perishable);
        Assert.Equal(Delivery.Reliable, perishable.Value.Delivery);
    }
}
