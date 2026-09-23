using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping;
using ReadyM.Api.Mapping.CreateDestroy;
using ReadyM.Api.Mapping.Policies.Data;
using ReadyM.Api.Mapping.Policies.Event;
using ReadyM.Api.Mapping.Tags;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// Which of a component's policy questions the client asks for each kind of write. Getting these
/// two the wrong way round is invisible everywhere else: the write still lands, just under the
/// wrong authority.
public class ClientWritePolicyTests
{
    private readonly EntityStore _store = new();

    private RawEntity AnEntity() => _store.CreateEntity(new TelemetryComponent()).RawEntity;

    private ClientEntityApi ApiWith(IMappingDataPolicy<Entity>? policy)
        => new(_store, policy is null ? null : new Lazy<IMappingPolicyDirectory>(() => new Directory(policy)));

    /// The question a mirror asks is whether the game may copy into the ECS.
    [Fact]
    public void A_mirror_asks_whether_the_game_may_copy_in()
    {
        var api = ApiWith(new Policy { GameCopiesIn = true, SetsFromApi = false });

        Assert.True(api.Allows(AnEntity(), typeof(TelemetryComponent), WriteKind.Mirror));
        Assert.False(api.Allows(AnEntity(), typeof(TelemetryComponent), WriteKind.Override));
    }

    /// And an override asks whether the API may set it, which a server-authoritative component
    /// allows on an entity you own while refusing the mirror.
    [Fact]
    public void An_override_asks_whether_the_api_may_set_it()
    {
        var api = ApiWith(new Policy { GameCopiesIn = false, SetsFromApi = true });

        Assert.False(api.Allows(AnEntity(), typeof(TelemetryComponent), WriteKind.Mirror));
        Assert.True(api.Allows(AnEntity(), typeof(TelemetryComponent), WriteKind.Override));
    }

    /// Nothing maps it to the game, so nothing can refuse a write or overwrite it afterwards.
    [Fact]
    public void A_component_no_policy_covers_takes_both()
    {
        var api = ApiWith(new Policy());
        var uncovered = typeof(MotionComponent);

        Assert.True(api.Allows(AnEntity(), uncovered, WriteKind.Mirror));
        Assert.True(api.Allows(AnEntity(), uncovered, WriteKind.Override));
    }

    /// What a test or a benchmark builds, where no directory exists at all.
    [Fact]
    public void A_client_with_no_policies_takes_both()
    {
        var api = ApiWith(null);

        Assert.True(api.Allows(AnEntity(), typeof(TelemetryComponent), WriteKind.Mirror));
        Assert.True(api.Allows(AnEntity(), typeof(TelemetryComponent), WriteKind.Override));
    }

    /// The client marks overrides, because it has a game that would otherwise overwrite them.
    [Fact]
    public void A_client_marks_its_overrides() => Assert.True(ApiWith(null).MarksOverrides);

    private sealed class Policy : IMappingDataPolicy<Entity>
    {
        public bool GameCopiesIn { get; init; }

        public bool SetsFromApi { get; init; }

        public bool ShouldGameCopyToEcs(in Entity context) => GameCopiesIn;

        public bool CanSetFromApi(in Entity context) => SetsFromApi;

        public bool ShouldEcsCopyToGame(in Entity context) => !GameCopiesIn;

        public bool CanGameSetLocally(in Entity context) => GameCopiesIn;
    }

    /// Answers for one component and, like the real one, refuses anything else.
    private sealed class Directory(IMappingDataPolicy<Entity> policy) : IMappingPolicyDirectory
    {
        public IMappingDataPolicy<Entity> ForData(Type componentType)
            => componentType == typeof(TelemetryComponent)
                ? policy
                : throw new ArgumentException($"No data policy registered for data type {componentType}");

        public IMappingCreateDeletePolicy<TGameObject> ForCreateDelete<TGameObject>(ArchetypeId archetypeId)
            where TGameObject : class
            => throw new NotSupportedException();

        public IMappingDataPolicy<TContext> ForData<TComponent, TContext>()
            where TComponent : struct, IMappingContext<TContext>
            => throw new NotSupportedException();

        public IMappingDataPolicy<Entity> ForData<TComponent>()
            where TComponent : struct, IMappingContext<Entity>
            => throw new NotSupportedException();

        public IMappingEventPolicy<TContext> ForEvent<TEvent, TContext>()
            where TEvent : struct, IMappingContext<TContext>
            => throw new NotSupportedException();

        public IMappingEventPolicy<TContext> ForEvent<TContext>(Type eventType) => throw new NotSupportedException();

        public IMappingEventPolicy<Entity> ForEvent<TEvent>()
            where TEvent : struct, IMappingContext<Entity>
            => throw new NotSupportedException();

        public IMappingEventPolicy<Entity> ForEvent(Type eventType) => throw new NotSupportedException();
    }
}
