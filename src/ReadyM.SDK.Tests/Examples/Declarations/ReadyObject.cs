using Friflo.Engine.ECS;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Archetypes.Attributes;
using ReadyM.SDK.Entity;

namespace ReadyM.SDK.Tests.Examples.Declarations;

[Archetype]
public readonly partial struct ReadyObject;

#region Generated

public readonly partial struct ReadyObject : IArchetype
{
    private readonly EntityHandle _handle;

    EntityHandle IArchetypeQueryable.Handle
    {
        get => _handle;
        init => _handle = value;
    }

    public ReadyObject(EntityHandle handle)
    {
        _handle = handle;
    }

    public ComponentSet Components => ComponentSet.Empty;

    public bool IsValid => _handle.IsAlive();

    public bool Has<T>() where T : struct, ITag => _handle.HasTag<T>();

    public void Set<T>(bool set) where T : struct, ITag
    {
        _handle.SetTag<T>(set);
    }
    
    public bool Is<T>() where T : struct, IArchetype => _handle.Is<T>();
    
    public bool TryAs<T>(out T archetype) where T : struct, IArchetype => _handle.TryAs(out archetype);
}

#endregion