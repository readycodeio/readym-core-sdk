using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping;
using ReadyM.Api.Mapping.CreateDestroy;
using ReadyM.Api.Mapping.Policies.Data;
using ReadyM.Api.Mapping.Policies.Event;
using ReadyM.Api.Mapping.Tags;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// A component's policy and the directory holding it, so a test can drive the client's real write
/// rule rather than restate it in a fake.
internal sealed class Policy : IMappingDataPolicy<Entity>
{
    public bool GameCopiesIn { get; init; }

    public bool SetsFromApi { get; init; }

    public bool ShouldGameCopyToEcs(in Entity context) => GameCopiesIn;

    public bool CanSetFromApi(in Entity context) => SetsFromApi;

    public bool ShouldEcsCopyToGame(in Entity context) => !GameCopiesIn;

    public bool CanGameSetLocally(in Entity context) => GameCopiesIn;
}

/// Answers for the components a write test goes through and, like the real one, refuses anything else.
internal sealed class Directory(IMappingDataPolicy<Entity> policy) : IMappingPolicyDirectory
{
    public IMappingDataPolicy<Entity> ForData(Type componentType)
        => componentType == typeof(TelemetryComponent) || componentType == typeof(RosterComponent)
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
