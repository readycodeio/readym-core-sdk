using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Attributes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Tests.Client.Fixtures;

/// An archetype whose components are a subset of Monster's and Chest's, which is what makes the
/// difference between "is this archetype" and "has these components" observable.
[Archetype]
[Include(typeof(Placement))]
public readonly partial struct Prop;

#region Generated

public readonly partial struct Prop : IArchetype
{
    private readonly EntityHandle _handle;

    public Prop(EntityHandle handle) => _handle = handle;

    EntityHandle IArchetypeQueryable.Handle
    {
        get => _handle;
        init => _handle = value;
    }

    public ComponentSet Components => ComponentSet.Of<PlacementComponent>();

    public bool IsValid => _handle.IsAlive();

    public bool Has<T>() where T : struct, ITag => _handle.HasTag<T>();

    public void Set<T>(bool set) where T : struct, ITag => _handle.SetTag<T>(set);
    
    public bool Is<T>() where T : struct, IArchetype => _handle.Is<T>();
    
    public bool TryAs<T>(out T archetype) where T : struct, IArchetype => _handle.TryAs(out archetype);
    
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
