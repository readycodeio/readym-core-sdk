using Friflo.Engine.ECS;
using ReadyM.SDK.Attributes;

namespace ReadyM.SDK.Tests.Examples.Declarations;

[Archetype]
[Include(typeof(Hostile), Optional: true)]
public readonly partial struct Npc;

#region Generated

public readonly partial struct Npc : IArchetype
{
    private readonly EntityHandle _handle;

    EntityHandle IArchetype.Handle => _handle;

    public Npc(EntityHandle handle)
    {
        _handle = handle;
    }
    
    public bool IsValid => _handle.IsAlive();

    public bool Has<T>() where T : struct, ITag => _handle.HasTag<T>();

    public void Set<T>(bool set) where T : struct, ITag
    {
        _handle.SetTag<T>(set);
    }

    // Hostile

    // end Hostile
}

#endregion