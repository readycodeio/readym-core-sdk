using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Server;
using ReadyM.SDK.Tests.Client.Fixtures;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Tests.Server;

/// An entity the host built from one of the game's own archetypes, which never goes through the
/// entity API. The world entity is the one that matters: a mod extends it and expects its defaults.
public class HostBuiltEntityTests
{
    private readonly EntityStore _store = new();

    /// Stands in for the host telling the SDK about an entity it built, which is all it can do: it
    /// knows nothing of the shapes a mod added to that archetype.
    private sealed class Components(EntityStore store) : IComponentsById
    {
        public bool Has<T>(int entityId) where T : struct, IComponent
            => store.GetEntityById(entityId).HasComponent<T>();

        public ref T Ref<T>(int entityId) where T : struct, IComponent
            => ref store.GetEntityById(entityId).GetComponent<T>();
    }

    private static T Shaped<T>(Entity entity, IEntityApi api) where T : struct, IArchetypeQueryable
        => new() { Handle = new EntityHandle(entity.RawEntity, api) };

    [Fact]
    public void A_host_built_entity_runs_what_its_shapes_asked_for()
    {
        var api = new ClientEntityApi(_store, NullLogger<ClientEntityApi>.Instance);
        var entity = _store.CreateEntity(new StampedComponent());

        ExtensionNativeInit.Use(api);
        ExtensionNativeInit.Run(entity.RawEntity, new Components(_store), local: true);

        Assert.Equal(7, Shaped<Stamped>(entity, api).Mark);
    }

    /// An entity that arrived from elsewhere was authored there, so its defaults are not ours to set.
    [Fact]
    public void One_that_arrived_from_elsewhere_is_left_alone()
    {
        var api = new ClientEntityApi(_store, NullLogger<ClientEntityApi>.Instance);
        var entity = _store.CreateEntity(new StampedComponent());

        ExtensionNativeInit.Use(api);
        ExtensionNativeInit.Run(entity.RawEntity, new Components(_store), local: false);

        Assert.Equal(0, Shaped<Stamped>(entity, api).Mark);
    }

    /// It carries none of the shapes that asked for anything, so nothing runs and nothing throws.
    [Fact]
    public void One_carrying_nothing_a_mod_declared_is_left_alone()
    {
        var api = new ClientEntityApi(_store, NullLogger<ClientEntityApi>.Instance);
        var entity = _store.CreateEntity(new ChestComponent());

        ExtensionNativeInit.Use(api);
        ExtensionNativeInit.Run(entity.RawEntity, new Components(_store), local: true);

        Assert.Equal(0, Shaped<Chest>(entity, api).Gold);
    }
}
