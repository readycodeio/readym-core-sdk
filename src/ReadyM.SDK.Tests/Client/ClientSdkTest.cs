using Friflo.Engine.ECS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReadyM.Api.DI;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client.Entities;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Services;
using ReadyM.SDK.Tests.Client.Fixtures;

namespace ReadyM.SDK.Tests.Client;

/// <summary>
/// One world and the V1 client services over it, per test.
/// </summary>
/// <remarks>
/// The container holds only what a mod needs to reach entities: the store, the entity API that
/// resolves handles against it, and the entity service. Attributed archetypes, mixins and tags are
/// meant to be discovered when a mod loads, which is not written yet, so the fixtures under
/// <c>Fixtures/</c> stand in for the generated code and nothing registers them.
/// </remarks>
public abstract class ClientSdkTest : IDisposable
{
    private readonly TestContainer _container;

    protected ClientSdkTest()
    {
        _container = new TestContainer();
        _container.Init();

        // Init wires ILogger<> to Logger<>, which needs a factory to come from. A game supplies a
        // real one; nothing here reads the output, so a null one keeps the graph resolvable.
        _container.RegisterSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        _container.RegisterSingleton<ILogger>(NullLogger.Instance);

        // What a create handler's parameters are resolved from, and the services a handler declared
        // in one is resolved out of. RegisterReadyMSdk does both for a game; this harness builds the
        // graph by hand, so it says the same thing itself.
        CreateHandlerRegistry.Use(_container);
        ServiceRegistry.RegisterAll(_container);

        _container.RegisterSingleton(new EntityStore());
        _container.RegisterSingleton<IEntityApi, ClientEntityApi>();
        _container.RegisterSingleton<IEntities, ClientEntities>();

        Store = _container.Resolve<EntityStore>();
        Api = _container.Resolve<IEntityApi>();
        Entities = _container.Resolve<IEntities>();
    }

    protected IDependencyContainer Container => _container;

    protected EntityStore Store { get; }

    internal IEntityApi Api { get; }

    protected IEntities Entities { get; }

    /// <summary>
    /// Creates an entity carrying exactly what the archetype needs, without naming its components.
    /// </summary>
    /// <remarks>
    /// The typed helpers below write component values directly, which only works for components this
    /// assembly declared. This one goes through the component set, so it also works for an archetype
    /// whose components belong to another assembly.
    /// </remarks>
    protected T Spawn<T>() where T : struct, IArchetypeQueryable
    {
        var archetype = Store.GetArchetype(ClientComponents.Resolve(ComponentsOf<T>()));
        return new T { Handle = new EntityHandle(archetype.CreateEntity().RawEntity, Api) };
    }

    protected Monster SpawnMonster(int level = 1, float hp = 50f, float maxHp = 100f, float x = 0f, float y = 0f)
    {
        var monster = Entities.Create<Monster>();

        monster.Level = level;
        monster.Hp = hp;
        monster.MaxHp = maxHp;
        monster.X = x;
        monster.Y = y;

        return monster;
    }

    /// <summary>Loot is a shape of its own, so asking for it spawns the narrower archetype.</summary>
    protected Chest SpawnChest(int gold = 10, float hp = 20f, float maxHp = 20f, float x = 0f, float y = 0f, int? rarity = null)
    {
        var chest = rarity.HasValue ? Entities.Create<LootedChest>() : Entities.Create<Chest>();

        chest.Gold = gold;
        chest.Hp = hp;
        chest.MaxHp = maxHp;
        chest.X = x;
        chest.Y = y;

        if (rarity.HasValue && EntityHandle.Of(chest).TryAs<Loot>(out var loot))
            loot.Rarity = rarity.Value;

        return chest;
    }

    protected Prop SpawnProp(float x = 0f, float y = 0f)
    {
        var prop = Entities.Create<Prop>();

        prop.X = x;
        prop.Y = y;

        return prop;
    }

    /// <summary>Components is an explicit interface implementation, so it is reached through the constraint.</summary>
    protected static ComponentSet ComponentsOf<T>() where T : struct, IArchetypeQueryable => default(T).Components;

    protected int Count<T>(EntityQuery<T> query) where T : struct, IArchetypeQueryable
    {
        var count = 0;
        foreach (var _ in query) count++;
        return count;
    }

    public void Dispose() => _container.Dispose();

    private sealed class TestContainer : DependencyContainerBase;
}
