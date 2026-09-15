using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Attributes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Tests.Client.Fixtures;

[ArchetypeMixin]
public readonly partial struct Placement
{
    public partial float X { get; set; }
    public partial float Y { get; set; }
}

#region Generated

internal struct PlacementComponent : IComponent
{
    public float x;
    public float y;
}

public readonly partial struct Placement(EntityHandle handle) : IArchetypeMixin
{
    EntityHandle IArchetypeQueryable.Handle
    {
        get => handle;
        init => handle = value;
    }

    public ComponentSet Components => ComponentSet.Of<PlacementComponent>();

    public partial float X
    {
        get => handle.GetComponent<PlacementComponent>().x;
        set => handle.GetComponent<PlacementComponent>().x = value;
    }

    public partial float Y
    {
        get => handle.GetComponent<PlacementComponent>().y;
        set => handle.GetComponent<PlacementComponent>().y = value;
    }
}

#endregion
