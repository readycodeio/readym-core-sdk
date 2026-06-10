using System;
using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Worlds;

public abstract class EntityBuilderBase
{
    public abstract EntityBuilderBase Add<T>() where T : struct, IComponent;
    public abstract EntityBuilderBase Add<T>(in T component) where T : struct, IComponent;

    internal virtual EntityBuilderBase Add(int structIndex, int stride)
    {
        throw new NotImplementedException();
    }

    internal virtual EntityBuilderBase AddTag<T>() where T : struct, ITag
    {
        throw new NotImplementedException();
    }
}