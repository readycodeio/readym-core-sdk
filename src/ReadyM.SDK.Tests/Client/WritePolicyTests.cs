using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// What a write through a replicated shape does on a client, where the component's policy decides
/// whether it lands and what it means. A refused write changes nothing and says so, which is the
/// half that would otherwise go unnoticed.
public class WritePolicyTests : ClientSdkTest
{
    private static bool DirtyOf(Rig rig) => EntityHandle.Of(rig).GetComponent<TelemetryComponent>().IsDirty;

    private static bool FromApiOf(Rig rig) => EntityHandle.Of(rig).GetComponent<TelemetryComponent>().ChangedFromApi;

    /// The same entity, reached through an API that answers differently. Handle is an explicit
    /// interface implementation, so it is reached through the constraint rather than on the type.
    private static T As<T>(in T shape, IEntityApi api) where T : struct, IArchetypeQueryable
        => new() { Handle = new EntityHandle(EntityHandle.Of(shape).RawEntity, api) };

    [Fact]
    public void A_refused_mirror_leaves_the_value_alone()
    {
        var rig = Entities.Create<Rig>();

        As(rig, new Deciding(Api, allows: false)).Ticks = 5;

        Assert.Equal(0, rig.Ticks);
        Assert.False(DirtyOf(rig));
    }

    [Fact]
    public void A_refused_override_leaves_the_value_alone()
    {
        var rig = Entities.Create<Rig>();

        As(rig, new Deciding(Api, allows: false)).Override(Telemetry.Field.Ticks, 5);

        Assert.Equal(0, rig.Ticks);
        Assert.False(FromApiOf(rig));
    }

    /// Reporting what the game did: it replicates, and nothing claims the value from the game.
    [Fact]
    public void An_allowed_mirror_writes_without_claiming_the_value()
    {
        var rig = Entities.Create<Rig>();

        As(rig, new Deciding(Api, allows: true, marks: true)).Ticks = 5;

        Assert.Equal(5, rig.Ticks);
        Assert.True(DirtyOf(rig));
        Assert.False(FromApiOf(rig));
    }

    [Fact]
    public void An_allowed_override_claims_the_value()
    {
        var rig = Entities.Create<Rig>();

        As(rig, new Deciding(Api, allows: true, marks: true)).Override(Telemetry.Field.Ticks, 5);

        Assert.Equal(5, rig.Ticks);
        Assert.True(DirtyOf(rig));
        Assert.True(FromApiOf(rig));
    }

    /// A server has no game to be overwritten by, so an override there is an ordinary write.
    [Fact]
    public void An_override_where_nothing_marks_them_is_an_ordinary_write()
    {
        var rig = Entities.Create<Rig>();

        As(rig, new Deciding(Api, allows: true, marks: false)).Override(Telemetry.Field.Ticks, 5);

        Assert.Equal(5, rig.Ticks);
        Assert.True(DirtyOf(rig));
        Assert.False(FromApiOf(rig));
    }

    [Fact]
    public void The_kind_reaches_the_api()
    {
        var rig = Entities.Create<Rig>();
        var deciding = new Deciding(Api, allows: true, marks: true);

        As(rig, deciding).Ticks = 1;
        As(rig, deciding).Override(Telemetry.Field.Ticks, 2);

        Assert.Equal([WriteKind.Mirror, WriteKind.Override], deciding.Asked);
    }

    /// Everything an entity needs, with the write decision answered by the test rather than by a
    /// policy directory the client would hold.
    private sealed class Deciding(IEntityApi inner, bool allows, bool marks = true) : IEntityApi
    {
        public List<WriteKind> Asked { get; } = [];

        public bool Allows(RawEntity rawEntity, Type component, WriteKind kind)
        {
            Asked.Add(kind);
            return allows;
        }

        public bool MarksOverrides => marks;

        public bool ShouldApplyToGame(RawEntity rawEntity, Type component) => !allows;

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
