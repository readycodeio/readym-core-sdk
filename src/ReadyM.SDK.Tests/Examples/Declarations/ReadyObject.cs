using Friflo.Engine.ECS;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.Examples.Declarations;

[Archetype]
public readonly partial struct ReadyObject;

#region Generated

public readonly partial struct ReadyObject : IArchetype
{
    private readonly EntityHandle _handle;
    EntityHandle IArchetype.Handle => _handle;

    public ReadyObject(EntityHandle handle)
    {
        _handle = handle;
    }
    
    public bool IsValid => _handle.IsAlive();

    public bool Has<T>() where T : struct, ITag => _handle.HasTag<T>();

    public void Set<T>(bool set) where T : struct, ITag
    {
        _handle.SetTag<T>(set);
    }
}

#endregion