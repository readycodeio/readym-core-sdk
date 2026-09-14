using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes.Attributes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Tests.Examples.Declarations;

[ArchetypeMixin]
public ref partial struct Vitals
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

public ref partial struct Vitals(RawEntity entity, IEntityApi entityApi)
{
    public partial float Hp
    {
        get => entityApi.GetComponent<VitalsComponent>(entity).hp;
        set => entityApi.GetComponent<VitalsComponent>(entity).hp = value;
    }

    public partial float MaxHp
    {
        get => entityApi.GetComponent<VitalsComponent>(entity).maxHp;
        set => entityApi.GetComponent<VitalsComponent>(entity).maxHp = value;
    }

    public partial bool IsDead => entityApi.GetComponent<VitalsComponent>(entity).isDead;
}

#endregion