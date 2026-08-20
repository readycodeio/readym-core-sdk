using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Worlds;

public abstract class EntityBuilderBase
{
    public abstract EntityBuilderBase Add<T>(in T component)
        where T : struct, IComponent;
    public abstract EntityBuilderBase AddTag<T>()
        where T : struct, ITag;
}
