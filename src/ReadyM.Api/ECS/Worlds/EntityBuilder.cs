using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Worlds;

internal sealed class EntityBuilder : EntityBuilderBase
{
    private readonly CreateEntityBatch _wrapped;

    internal EntityBuilder(CreateEntityBatch wrapped)
    {
        _wrapped = wrapped;
    }

    public override EntityBuilderBase Add<T>(in T value)
    {
        _wrapped.Add<T>(value);
        return this;
    }

    public override EntityBuilderBase AddTag<T>()
    {
        _wrapped.AddTag<T>();
        return this;
    }
}
