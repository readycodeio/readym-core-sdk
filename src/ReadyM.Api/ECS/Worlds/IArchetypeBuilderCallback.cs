using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Worlds;

public interface IArchetypeBuilderCallback
{
    void AcceptComponentType<T>(ArchetypeBuilder builder)
        where T : struct, IComponent;

    void AcceptComponentType<T>(ArchetypeBuilder builder, T defaultValue)
        where T : struct, IComponent;

    void AcceptStrideComponent(ArchetypeBuilder builder, int structIndex, int stride);

    void AcceptTag<T>(ArchetypeBuilder builder)
        where T : struct, ITag;
}
