using Friflo.Engine.ECS;

namespace ReadyM.SDK.Archetypes;

public interface IArchetype : IArchetypeQueryable
{
    bool IsValid { get; }

    bool Has<T>() where T : struct, ITag;

    void Set<T>(bool set) where T : struct, ITag;

    bool Is<T>() where T : struct, IArchetype;
    
    bool TryAs<T>(out T archetype) where T : struct, IArchetype;
}