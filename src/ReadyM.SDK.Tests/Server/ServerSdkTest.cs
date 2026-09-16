using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;
using ReadyM.SDK.Server.Entity;
using ReadyM.SDK.TestHarness;
using ReadyM.SDK.Tests.Server.Fixtures;
using IComponent = Friflo.Engine.ECS.IComponent;
using ServerEntities = ReadyM.SDK.Server.Entity.Entities;

namespace ReadyM.SDK.Tests.Server;

/// <summary>
/// A fake relay and the V1 server services over it, per test.
/// </summary>
/// <remarks>
/// Built by hand rather than through a container, because both the api and the component registry
/// are constructed from interop pointer bundles rather than from resolvable dependencies. The two
/// heap kinds are split across the fixture components on purpose, so an ordinary test covers both
/// branches of slot resolution without saying so.
/// </remarks>
public abstract class ServerSdkTest
{
    protected ServerSdkTest()
    {
        Relay = new FakeRelay();

        // Split across both heap kinds on purpose, so an ordinary test covers both branches of
        // slot resolution without saying so.
        Relay.RegisterBlittable<PositionComponent>();
        Relay.RegisterManaged<VitalsComponent>();
        Relay.RegisterManaged<NamedComponent>();
        Relay.RegisterBlittable<NpcComponent>();
        Relay.RegisterManaged<BoulderComponent>();
        Relay.RegisterBlittable<SpeedComponent>();
        Relay.RegisterManaged<WealthComponent>();
        Relay.RegisterBlittable<MoodComponent>();

        // Everything else a fixture needs, which is the archetype markers and any component
        // belonging to another assembly. Whatever is already registered above keeps its kind.
        foreach (var component in ComponentsOfEveryFixture())
            Relay.RegisterUnnamed(component);

        Api = Relay.CreateEntityApi();
        Entities = new ServerEntities(Api);
    }

    /// Found rather than listed, so a new fixture does not need remembering here.
    private static IEnumerable<Type> ComponentsOfEveryFixture()
        => typeof(Npc).Assembly
            .GetTypes()
            .Where(type => type.IsValueType && typeof(IComponent).IsAssignableFrom(type))
            .Concat(ComponentsOf<Peddler>().Types);

    internal FakeRelay Relay { get; }

    internal ServerEntityApi Api { get; }

    protected IEntities Entities { get; }

    /// Creates an entity carrying exactly what the archetype needs, without naming its components.
    protected T Spawn<T>() where T : struct, IArchetypeQueryable
        => Wrap<T>(Relay.Create(ComponentsOf<T>().Types));

    protected T Wrap<T>(RawEntity entity) where T : struct, IArchetypeQueryable
        => new() { Handle = new EntityHandle(entity, Api) };

    protected static RawEntity IdentityOf<T>(in T archetype) where T : struct, IArchetypeQueryable
        => EntityHandle.Of(archetype).RawEntity;

    /// Components is an explicit interface implementation, so it is reached through the constraint.
    protected static ComponentSet ComponentsOf<T>() where T : struct, IArchetypeQueryable => default(T).Components;

    /// <summary>
    /// Counts by walking identities, whatever the shape supports.
    /// </summary>
    /// <remarks>
    /// T is a type parameter here, not a concrete shape, so the foreach binds the general extension
    /// rather than a generated one. Which path a loop takes is decided where the shape is named.
    /// </remarks>
    protected int CountIdentities<T>(EntityQuery<T> query) where T : struct, IArchetypeQueryable
    {
        var count = 0;

        foreach (var _ in query)
            count++;

        return count;
    }
}
