using Friflo.Engine.ECS;

namespace ReadyM.SDK;

public interface IArchetype
{
    EntityHandle Handle { get; }

    bool IsValid { get; }

    bool Has<T>() where T : struct, ITag;

    void Set<T>(bool set) where T : struct, ITag;
}