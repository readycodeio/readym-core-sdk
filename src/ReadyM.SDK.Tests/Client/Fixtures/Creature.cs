using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Attributes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Tests.Client.Fixtures;

/// The archetype Monster includes, which is what gives Monster a conversion up to it.
[Archetype]
[Include(typeof(Health))]
public readonly partial struct Creature;

#region Generated

public readonly partial struct Creature : IArchetype
{
    private readonly EntityHandle _handle;

    public Creature(EntityHandle handle) => _handle = handle;

    EntityHandle IArchetypeQueryable.Handle
    {
        get => _handle;
        init => _handle = value;
    }

    public ComponentSet Components => ComponentSet.Of<HealthComponent>();

    public bool IsValid => _handle.IsAlive();

    public bool Has<T>() where T : struct, ITag => _handle.HasTag<T>();

    public void Set<T>(bool set) where T : struct, ITag => _handle.SetTag<T>(set);
    
    public bool Is<T>() where T : struct, IArchetype => _handle.Is<T>();
    
    public bool TryAs<T>(out T archetype) where T : struct, IArchetype => _handle.TryAs(out archetype);
    
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
}

#endregion
