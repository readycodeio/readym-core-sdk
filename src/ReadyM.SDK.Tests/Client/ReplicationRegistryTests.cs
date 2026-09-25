
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

    /// A shape over storage that already exists replicates through it, but does not register it:
    /// that component belongs to whoever declared it, and may be one the host registered itself.
    /// Registering it again as a mod component is wrong, and for a large one the host refuses it.
    [Fact]
    public void A_shape_over_a_component_it_does_not_own_registers_nothing()
    {
        ReplicationRegistry.Clear();
        global::ReadyM.SDK.Generated.ShapeRegistrations.RegisterAll();

        Assert.Null(Find(typeof(global::ReadyM.Api.Multiplayer.ECS.Components.EmptyScopeDeletionComponent)));

        // Nothing was generated to register it, unlike a shape whose component the SDK made.
        Assert.Null(RegistrationFor<Perishable>());
        Assert.NotNull(RegistrationFor<Telemetry>());
    }

    /// The generated class that tells the host about a shape's component, when the shape owns one.
    private static Type? RegistrationFor<T>() where T : struct
        => typeof(T).Assembly.GetType($"{typeof(T).Namespace}.{typeof(T).Name}Replication");

    /// What the SDK tells the host about for an archetype the host owns: the components it
    /// generated, never the ones it only borrowed. World has a marker of its own; Area, Player and
    /// Cell sit on components the host declared and registers itself.
    [Fact]
    public void Only_a_shapes_generated_components_are_the_sdks_to_register()
    {
        Assert.Equal(
            [typeof(global::ReadyM.SDK.Core.World).Assembly.GetType("ReadyM.SDK.Core.WorldArchetypeMarker")],
            Generated<global::ReadyM.SDK.Core.World>());

        Assert.Empty(Generated<global::ReadyM.SDK.Core.Area>());
        Assert.Empty(Generated<global::ReadyM.SDK.Core.Player>());
        Assert.Empty(Generated<global::ReadyM.SDK.Core.Cell>());
    }

    /// Mirrors the rule ServerArchetypes applies, which cannot be reached from here: a generated
    /// component lands in the shape's own assembly, a borrowed one does not.
    private static Type[] Generated<T>() where T : struct, IArchetypeQueryable
        => [.. ((IArchetypeQueryable)default(T)).Components.Types.Where(c => c.Assembly == typeof(T).Assembly)];
}
