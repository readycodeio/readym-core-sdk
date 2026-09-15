using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Attributes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Tests.Examples.Declarations;

[ArchetypeMixin]
public readonly partial struct Vitals
{
    public partial float Hp { get; set; }
    public partial float MaxHp { get; set; }
    public partial bool IsDead { get; } // read-only accessor: no setter generated
}

#region Generated

internal struct VitalsComponent : IComponent
{
    public float hp;
    public float maxHp;
    public bool isDead;
}

public readonly partial struct Vitals(EntityHandle handle) : IArchetypeMixin
{
    EntityHandle IArchetypeQueryable.Handle
    {
        get  => handle;
        init  => handle = value;
    }

    public ComponentTypes ComponentTypes => ComponentTypes.Get<VitalsComponent>();
    
    public partial float Hp
    {
        get => handle.GetComponent<VitalsComponent>().hp;
        set => handle.GetComponent<VitalsComponent>().hp = value;
    }

    public partial float MaxHp
    {
        get => handle.GetComponent<VitalsComponent>().maxHp;
        set => handle.GetComponent<VitalsComponent>().maxHp = value;
    }

    public partial bool IsDead => handle.GetComponent<VitalsComponent>().isDead;
}

#endregion