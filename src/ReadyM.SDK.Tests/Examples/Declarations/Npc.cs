using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Attributes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Tests.Examples.Declarations;

[Archetype]
[Include(typeof(VitalsComponent))]
[Include(typeof(Hostile), Optional: true)]
public readonly partial struct Npc;

#region Generated

public readonly partial struct Npc : IArchetype
{
    private readonly EntityHandle _handle;

    EntityHandle IArchetype.Handle
    {
        get => _handle;
        init => _handle = value;
    }

    public Npc(EntityHandle handle)
    {
        _handle = handle;
    }

    public bool IsValid => _handle.IsAlive();

    public ComponentTypes ComponentTypes => new();

    public bool Has<T>() where T : struct, ITag => _handle.HasTag<T>();

    public void Set<T>(bool set) where T : struct, ITag
    {
        _handle.SetTag<T>(set);
    }
    
    // vitals

    public float Hp
    {
        get => _handle.GetComponent<VitalsComponent>().hp;
        set => _handle.GetComponent<VitalsComponent>().hp = value;
    }

    public float MaxHp
    {
        get => _handle.GetComponent<VitalsComponent>().maxHp;
        set => _handle.GetComponent<VitalsComponent>().maxHp = value;
    }

    public bool IsDead => _handle.GetComponent<VitalsComponent>().isDead;

    // end vitals
}

#endregion