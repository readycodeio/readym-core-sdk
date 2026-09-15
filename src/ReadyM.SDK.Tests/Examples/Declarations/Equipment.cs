using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Attributes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Tests.Examples.Declarations;

[ArchetypeMixin]
public readonly partial struct Equipment
{
    public partial string Weapon { get; set; }
}

#region Generated

internal struct EquipmentComponent : IComponent
{
    public string weapon;
}

public readonly partial struct Equipment(EntityHandle handle) : IArchetypeMixin
{
    EntityHandle IArchetypeQueryable.Handle
    {
        get  => handle;
        init  => handle = value;
    }

    public ComponentTypes ComponentTypes => ComponentTypes.Get<EquipmentComponent>();
    
    public partial string Weapon
    {
        get => handle.GetComponent<EquipmentComponent>().weapon;
        set => handle.GetComponent<EquipmentComponent>().weapon = value;
    }
}

#endregion