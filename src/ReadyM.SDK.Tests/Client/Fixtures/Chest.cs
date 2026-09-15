using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Attributes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Tests.Client.Fixtures;

[Archetype]
[Include(typeof(Health))]
[Include(typeof(Placement))]
[Include(typeof(Loot), Optional: true)]
public readonly partial struct Chest
{
    public partial int Gold { get; set; }
}

#region Generated

internal struct ChestComponent : IComponent
{
    public int gold;
}

public readonly partial struct Chest : IArchetype
{
    private readonly EntityHandle _handle;

    public Chest(EntityHandle handle) => _handle = handle;

    EntityHandle IArchetypeQueryable.Handle
    {
        get => _handle;
        init => _handle = value;
    }

    public ComponentSet Components => ComponentSet.Of<ChestComponent, HealthComponent, PlacementComponent>();

    public bool IsValid => _handle.IsAlive();

    public bool Has<T>() where T : struct, ITag => _handle.HasTag<T>();

    public void Set<T>(bool set) where T : struct, ITag => _handle.SetTag<T>(set);

    public bool Is<T>() where T : struct, IArchetype => _handle.Is<T>();
    
    public bool TryAs<T>(out T archetype) where T : struct, IArchetype => _handle.TryAs(out archetype);

    public partial int Gold
    {
        get => _handle.GetComponent<ChestComponent>().gold;
        set => _handle.GetComponent<ChestComponent>().gold = value;
    }

    public float Hp
    {
        get => _handle.GetComponent<HealthComponent>().hp;
        set => _handle.GetComponent<HealthComponent>().hp = value;
    }

    public float MaxHp
    {
        get => _handle.GetComponent<HealthComponent>().maxHp;
        set => _handle.GetComponent<HealthComponent>().maxHp = value;
    }

    public int? Rarity => _handle.TryGetComponent<LootComponent>(out var loot) ? loot.rarity : null;

    public bool TryGetLoot(out Loot loot)
    {
        if (_handle.HasComponent<LootComponent>())
        {
            loot = new Loot(_handle);
            return true;
        }

        loot = default;
        return false;
    }

    public Loot RequireLoot()
    {
        if (!_handle.HasComponent<LootComponent>())
            throw new InvalidOperationException($"Entity {_handle.Id} does not carry Loot.");

        return new Loot(_handle);
    }

    public Loot EnsureLoot()
    {
        if (!_handle.HasComponent<LootComponent>())
            _handle.AddComponent<LootComponent>();

        return new Loot(_handle);
    }

    public float X
    {
        get => _handle.GetComponent<PlacementComponent>().x;
        set => _handle.GetComponent<PlacementComponent>().x = value;
    }

    public float Y
    {
        get => _handle.GetComponent<PlacementComponent>().y;
        set => _handle.GetComponent<PlacementComponent>().y = value;
    }
}

#endregion