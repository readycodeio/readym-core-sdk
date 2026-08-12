using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Worlds;

internal sealed class EntityBuilder : EntityBuilderBase
{
    private readonly CreateEntityBatch _wrapped;

    internal EntityBuilder(CreateEntityBatch wrapped)
    {
        _wrapped = wrapped;
    }

    public override EntityBuilderBase Add<T>()
    {
        return new EntityBuilder(_wrapped.Add<T>());
    }

    public override EntityBuilderBase Add<T>(in T component)
    {
        return new EntityBuilder(_wrapped.Add(in component));
    }

    internal override EntityBuilderBase Add(int structIndex, int stride)
    {
        return new EntityBuilder(_wrapped.Add(structIndex, stride));
    }

    internal override EntityBuilderBase AddTag<T>()
    {
        return new EntityBuilder(_wrapped.AddTag<T>());
    }
}