using Friflo.Engine.ECS;
using ReadyM.Api.DI;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Client.Entity;
using ReadyM.SDK.Entity;
using ReadyM.SDK.Tests.Client.Fixtures;
using ClientEntities = ReadyM.SDK.Client.Entity.Entities;

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

    protected Monster SpawnMonster(int level = 1, float hp = 50f, float maxHp = 100f, float x = 0f, float y = 0f)
    {
        var entity = Store.CreateEntity(
            new MonsterComponent { level = level },
            new HealthComponent { hp = hp, maxHp = maxHp },
            new PlacementComponent { x = x, y = y });

        return new Monster(new EntityHandle(entity.RawEntity, Api));
    }

    protected Chest SpawnChest(int gold = 10, float hp = 20f, float maxHp = 20f, float x = 0f, float y = 0f, int? rarity = null)
    {
        var entity = Store.CreateEntity(
            new ChestComponent { gold = gold },
            new HealthComponent { hp = hp, maxHp = maxHp },
            new PlacementComponent { x = x, y = y });

        if (rarity.HasValue)
            entity.AddComponent(new LootComponent { rarity = rarity.Value });

        return new Chest(new EntityHandle(entity.RawEntity, Api));
    }

    protected Prop SpawnProp(float x = 0f, float y = 0f)
    {
        var entity = Store.CreateEntity(new PlacementComponent { x = x, y = y });
        return new Prop(new EntityHandle(entity.RawEntity, Api));
    }

    /// <summary>
    /// Tags an entity straight in the store, for arranging a test before it runs a query.
    /// </summary>
    /// <remarks>
    /// This bypasses the command buffer, so it is an immediate structural change. Calling it inside
    /// a query loop is what <c>Set&lt;T&gt;</c> exists to avoid, and Friflo rejects it.
    /// </remarks>
    protected void AddTag<TTag, TArchetype>(in TArchetype entity)
        where TTag : struct, ITag
        where TArchetype : struct, IArchetypeQueryable
        => Store.GetEntityByRawEntity(EntityHandle.Of(entity).RawEntity).AddTag<TTag>();

    protected int Count<T>(QueryBuilder<T> query) where T : struct, IArchetypeQueryable
    {
        var count = 0;
        foreach (var _ in query) count++;
        return count;
    }

    public void Dispose() => _container.Dispose();

    private sealed class TestContainer : DependencyContainerBase;
}
