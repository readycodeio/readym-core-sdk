using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Attributes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Tests.Client.Fixtures;

[Archetype]
[IncludeArchetype(typeof(Creature))]
[Include(typeof(Health))]
[Include(typeof(Placement))]
[Include(typeof(Enraged), Optional: true)]
[Include(typeof(Dormant), Optional: true)]
public readonly partial struct Monster
{
    public partial int Level { get; set; }
}

#region Generated

internal struct MonsterComponent : IComponent
{
    public int level;
}

public readonly partial struct Monster : IArchetype
{
    private readonly EntityHandle _handle;

    public Monster(EntityHandle handle) => _handle = handle;

    EntityHandle IArchetypeQueryable.Handle
    {
        get => _handle;
        init => _handle = value;
    }

    public ComponentSet Components => ComponentSet.Of<MonsterComponent, HealthComponent, PlacementComponent>();

    public bool IsValid => _handle.IsAlive();

    public bool Has<T>() where T : struct, ITag => _handle.HasTag<T>();

    public void Set<T>(bool set) where T : struct, ITag => _handle.SetTag<T>(set);
    
    public bool Is<T>() where T : struct, IArchetype => _handle.Is<T>();
    
    public bool TryAs<T>(out T archetype) where T : struct, IArchetype => _handle.TryAs(out archetype);
    
    public static implicit operator Creature(Monster monster) => new(monster._handle);

    public static explicit operator Monster(Creature creature)
    {
        var handle = EntityHandle.Of(creature);

        if (!handle.Is<Monster>())
            throw new InvalidCastException($"The entity is not a {nameof(Monster)}.");

        return new Monster(handle);
    }

    public partial int Level
    {
        get => _handle.GetComponent<MonsterComponent>().level;
        set => _handle.GetComponent<MonsterComponent>().level = value;
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
