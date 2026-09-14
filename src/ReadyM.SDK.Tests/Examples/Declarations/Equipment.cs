using Friflo.Engine.ECS;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.Examples.Declarations;

[ArchetypeMixin]
public ref partial struct Equipment
{
    public partial string Weapon { get; set; }
}

#region Generated

internal struct EquipmentComponent : IComponent
{
    public string weapon;
}

public ref partial struct Equipment(EntityHandle handle)
{
    public partial string Weapon
    {
        get => handle.GetComponent<EquipmentComponent>().weapon;
        set => handle.GetComponent<EquipmentComponent>().weapon = value;
    }
}

#endregion