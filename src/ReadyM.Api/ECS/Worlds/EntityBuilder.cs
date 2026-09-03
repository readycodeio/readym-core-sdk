using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Worlds;

internal sealed class EntityBuilder
{
    private readonly CreateEntityBatch _wrapped;

    internal EntityBuilder(CreateEntityBatch wrapped)
    {
        _wrapped = wrapped;
    }

    public EntityBuilder Add<T>(in T value) where T : struct, IComponent
    {
        _wrapped.Add<T>(value);
        return this;
    }

    public EntityBuilder AddTag<T>() where T : struct, ITag
    {
        _wrapped.AddTag<T>();
        return this;
    }
}