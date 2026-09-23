using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client;
using ReadyM.SDK.Client.Mapping;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// Moving a value between the ECS and a game object at a sync point. The direction is the
/// component's policy to decide, and pushing is what marks an overridden value as shown.
public class ShapeMappingTests : ClientSdkTest
{
    /// Stands in for whatever the game holds the value in.
    private sealed class Dial
    {
        public int Reading { get; set; }
    }

    private static ShapeMappingRegistry Mapped()
    {
        var registry = new ShapeMappingRegistry();

        ((IShapeMappingRegistry)registry).For<Telemetry, Dial>()
            .Map(Telemetry.Field.Ticks,
                 push: (ticks, dial) => dial.Reading = ticks,
                 pull: (ref int ticks, Dial dial) => ticks = dial.Reading);

        SyncExtensions.Use(registry);
        return registry;
    }

    private static T As<T>(in T shape, IEntityApi api) where T : struct, IArchetypeQueryable
        => new() { Handle = new EntityHandle(EntityHandle.Of(shape).RawEntity, api) };

    private static bool OverriddenOf(Rig rig)
        => EntityHandle.Of(rig).GetComponent<TelemetryComponent>().ChangedFromApi;

    [Fact]
    public void The_ecs_value_reaches_the_game_when_it_drives()
    {
        Mapped();

        var rig = Entities.Create<Rig>();
        var dial = new Dial();

        rig.Ticks = 7;

        Assert.True(As(rig, new Deciding(Api, ecsDrives: true)).Push(Telemetry.Field.Ticks, dial));
        Assert.Equal(7, dial.Reading);
    }

    [Fact]
    public void The_game_value_reaches_the_ecs_when_it_drives()
    {
        Mapped();

        var rig = Entities.Create<Rig>();
        var dial = new Dial { Reading = 3 };

        Assert.True(As(rig, new Deciding(Api, ecsDrives: false)).Pull(Telemetry.Field.Ticks, dial));
        Assert.Equal(3, rig.Ticks);
    }

    /// The other direction is refused rather than silently doing the wrong thing.
    [Fact]
    public void Neither_direction_runs_against_the_policy()
    {
        Mapped();

        var rig = Entities.Create<Rig>();
        var dial = new Dial { Reading = 3 };

        Assert.False(As(rig, new Deciding(Api, ecsDrives: true)).Pull(Telemetry.Field.Ticks, dial));
        Assert.False(As(rig, new Deciding(Api, ecsDrives: false)).Push(Telemetry.Field.Ticks, dial));
        Assert.Equal(0, rig.Ticks);
        Assert.Equal(3, dial.Reading);
    }

    /// An override is the one thing that pushes on an entity the game otherwise drives, and the
    /// push is what clears the flag. Nothing asks the mod to.
    [Fact]
    public void An_override_pushes_once_and_then_stops()
    {
        Mapped();

        var rig = Entities.Create<Rig>();
        var dial = new Dial();
        var driving = new Deciding(Api, ecsDrives: false);

        As(rig, driving).Override(Telemetry.Field.Ticks, 42);

        Assert.True(OverriddenOf(rig));
        Assert.True(As(rig, driving).Push(Telemetry.Field.Ticks, dial));
        Assert.Equal(42, dial.Reading);

        Assert.False(OverriddenOf(rig));
        Assert.False(As(rig, driving).Push(Telemetry.Field.Ticks, dial));
    }

    /// Until it is pushed, the game must not report over the value the mod just claimed.
    [Fact]
    public void An_override_holds_off_the_game()
    {
        Mapped();

        var rig = Entities.Create<Rig>();
        var driving = new Deciding(Api, ecsDrives: false);

        As(rig, driving).Override(Telemetry.Field.Ticks, 42);

        Assert.False(As(rig, driving).Pull(Telemetry.Field.Ticks, new Dial { Reading = 3 }));
        Assert.Equal(42, rig.Ticks);
    }

    /// The plain setter is the game reporting what it did, and it must not report over a value the
    /// mod has claimed but the game has not been shown yet.
    [Fact]
    public void An_override_holds_off_a_plain_write()
    {
        Mapped();

        var rig = Entities.Create<Rig>();
        var driving = new Deciding(Api, ecsDrives: false);
        var claimed = As(rig, driving);

        claimed.Override(Telemetry.Field.Ticks, 42);
        claimed.Ticks = 3;

        Assert.Equal(42, rig.Ticks);

        // Once the game has been shown it, the game drives it again.
        claimed.Push(Telemetry.Field.Ticks, new Dial());
        claimed.Ticks = 3;

        Assert.Equal(3, rig.Ticks);
    }

    [Fact]
    public void A_context_nothing_mapped_moves_nothing()
    {
        Mapped();

        var rig = Entities.Create<Rig>();

        Assert.False(As(rig, new Deciding(Api, ecsDrives: true)).Push(Telemetry.Field.Ticks, "not a dial"));
    }

    // -- a correspondence that spans the shape's values ---------------------------------------------

    /// Stands in for a game object read or written in one go.
    private sealed class Panel
    {
        public int Ticks { get; set; }

        public float Load { get; set; }
    }

    private static void MappedWhole()
    {
        var registry = new ShapeMappingRegistry();

        ((IShapeMappingRegistry)registry).For<Telemetry, Panel>()
            .Map(Telemetry.Field.All,
                 push: (telemetry, panel) =>
                 {
                     panel.Ticks = telemetry.Ticks;
                     panel.Load = telemetry.Load;
                 },
                 pull: (telemetry, panel) =>
                 {
                     telemetry.Ticks = panel.Ticks;
                     telemetry.Load = panel.Load;
                 });

        SyncExtensions.Use(registry);
    }

    private static Telemetry AsTelemetry(Rig rig, IEntityApi api)
        => new EntityHandle(EntityHandle.Of(rig).RawEntity, api).As<Telemetry>();

    [Fact]
    public void A_shape_wide_push_shows_the_game_every_value()
    {
        MappedWhole();

        var rig = Entities.Create<Rig>();
        var panel = new Panel();

        rig.Ticks = 5;
        rig.Load = 0.5f;

        Assert.True(AsTelemetry(rig, new Deciding(Api, ecsDrives: true)).Push(Telemetry.Field.All, panel));
        Assert.Equal(5, panel.Ticks);
        Assert.Equal(0.5f, panel.Load);
    }

    [Fact]
    public void A_shape_wide_pull_reads_every_value_back()
    {
        MappedWhole();

        var rig = Entities.Create<Rig>();
        var panel = new Panel { Ticks = 9, Load = 1.5f };

        Assert.True(AsTelemetry(rig, new Deciding(Api, ecsDrives: false)).Pull(Telemetry.Field.All, panel));
        Assert.Equal(9, rig.Ticks);
        Assert.Equal(1.5f, rig.Load);
    }

    /// One value claimed is enough to push the shape, and the push releases the whole of it.
    [Fact]
    public void A_shape_wide_push_releases_the_claim()
    {
        MappedWhole();

        var rig = Entities.Create<Rig>();
        var driving = new Deciding(Api, ecsDrives: false);

        AsTelemetry(rig, driving).Override(Telemetry.Field.Ticks, 42);

        Assert.True(OverriddenOf(rig));
        Assert.True(AsTelemetry(rig, driving).Push(Telemetry.Field.All, new Panel()));
        Assert.False(OverriddenOf(rig));

        // And with nothing claimed, the game drives it again.
        Assert.False(AsTelemetry(rig, driving).Push(Telemetry.Field.All, new Panel()));
    }

    /// Answers the direction questions itself, standing in for the policies a client would hold.
    private sealed class Deciding(IEntityApi inner, bool ecsDrives) : IEntityApi
    {
        // The ownership policy in miniature: the side that does not drive the value from the game
        // is the side whose ECS is authoritative, and only the other may report or claim it.
        public bool Allows(RawEntity rawEntity, Type component, WriteKind kind) => !ecsDrives;

        public bool MarksOverrides => true;

        public bool ShouldApplyToGame(RawEntity rawEntity, Type component) => ecsDrives;

        public int ComponentIdOf(Type type) => inner.ComponentIdOf(type);

        public ComponentRef Locate(RawEntity rawEntity, int componentId) => inner.Locate(rawEntity, componentId);

        public void ReplaceIndexed<TComponent, TKey>(RawEntity rawEntity, TComponent component)
            where TComponent : struct, IIndexedComponent<TKey>
            => inner.ReplaceIndexed<TComponent, TKey>(rawEntity, component);

        public EntityBuffer CollectInScope(RawEntity scope, ComponentSet components)
            => inner.CollectInScope(scope, components);

        public bool TryFindByIndex<TComponent, TKey>(TKey key, out RawEntity entity)
            where TComponent : struct, IIndexedComponent<TKey>
            => inner.TryFindByIndex<TComponent, TKey>(key, out entity);

        public bool IsAlive(RawEntity rawEntity) => inner.IsAlive(rawEntity);

        public bool HasComponents(RawEntity rawEntity, ComponentSet components)
            => inner.HasComponents(rawEntity, components);

        public RawEntity Create(ComponentSet components) => inner.Create(components);

        public RawEntity Create(ComponentSet components, RawEntity scope) => inner.Create(components, scope);

        public bool Delete(RawEntity rawEntity) => inner.Delete(rawEntity);

        public void EnterQuery() => inner.EnterQuery();

        public void LeaveQuery() => inner.LeaveQuery();
    }
}
