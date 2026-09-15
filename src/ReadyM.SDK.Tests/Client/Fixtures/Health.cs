using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Attributes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Tests.Client.Fixtures;

[ArchetypeMixin]
public readonly partial struct Health
{
    public partial float Hp { get; set; }
    public partial float MaxHp { get; set; }
}

#region Generated

internal struct HealthComponent : IComponent
{
    public float hp;
    public float maxHp;
}

public readonly partial struct Health(EntityHandle handle) : IArchetypeMixin
{
    EntityHandle IArchetypeQueryable.Handle
    {
        get => handle;
        init => handle = value;
    }

    public ComponentSet Components => ComponentSet.Of<HealthComponent>();

    public partial float Hp
    {
        get => handle.GetComponent<HealthComponent>().hp;
        set => handle.GetComponent<HealthComponent>().hp = value;
    }

    public partial float MaxHp
    {
        get => handle.GetComponent<HealthComponent>().maxHp;
        set => handle.GetComponent<HealthComponent>().maxHp = value;
    }
}

#endregion
