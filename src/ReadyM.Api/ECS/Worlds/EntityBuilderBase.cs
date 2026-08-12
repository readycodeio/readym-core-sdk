using System;
using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Worlds;

public abstract class EntityBuilderBase
{
    /// <summary>
    /// Add a component to the archetype.
    /// </summary>
    /// <typeparam name="T">Type of the component. Must implement <see cref="IComponent" />.</typeparam>
    public abstract EntityBuilderBase Add<T>() where T : struct, IComponent;
    
    /// <summary>
    /// Add a component with a default value to the archetype. 
    /// </summary>
    /// <typeparam name="T">Type of the component. Must implement <see cref="IComponent" />.</typeparam>
    [Obsolete("Default values are ignored on the server-side. Use Add<T>() and set the values manually later.")]
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