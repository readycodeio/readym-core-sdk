using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Worlds;

/// <summary>
/// Builder class for creating archetypes in the ECS (Entity Component System) framework.
/// It allows adding components, tags, and custom filters to define the structure of an archetype.
/// </summary>
public class ArchetypeBuilder
{
    private readonly List<Action<IArchetypeBuilderCallback>> _acceptCallbacks = [];
    private readonly List<IArchetypeBuilderCallback> _filters = [];

    /// <summary>
    /// Adds a component type to the archetype builder. The component type must be a value type and implement the <see cref="IComponent"/> interface.
    /// </summary>
    internal ArchetypeBuilder Add(Type componentType)
    {
        if (!componentType.IsValueType)
            throw new ArgumentException($"Type {componentType} is not a value type.", nameof(componentType));
        if (!typeof(IComponent).IsAssignableFrom(componentType))
            throw new ArgumentException($"Type {componentType} does not implement IComponent.", nameof(componentType));

        var method = typeof(ArchetypeBuilder).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(m => m is { Name: nameof(Add), IsGenericMethodDefinition: true } && m.GetParameters().Length == 0);
        method = method.MakeGenericMethod(componentType);

        method.Invoke(this, null);
        return this;
    }

    /// <summary>
    /// Adds a component of type T to the archetype builder. The component type must be a value type and implement the <see cref="IComponent"/> interface.
    /// </summary>
    public ArchetypeBuilder Add<T>() where T : struct, IComponent
    {
        var accept = new Action<IArchetypeBuilderCallback>(callback =>
        {
            callback.AcceptComponentType<T>(this);
        });
        _acceptCallbacks.Add(accept);

        foreach (var filter in _filters)
        {
            accept(filter);
        }

        return this;
    }

    /// <summary>
    /// Adds a component of type T with a specific instance to the archetype builder. The component type must be a value type and implement the <see cref="IComponent"/> interface.
    /// </summary>
    public ArchetypeBuilder Add<T>(T component)
        where T : struct, IComponent
    {
        var accept = new Action<IArchetypeBuilderCallback>(callback =>
        {
            callback.AcceptComponentType(this, component);
        });
        _acceptCallbacks.Add(accept);

        foreach (var filter in _filters)
        {
            accept(filter);
        }

        return this;
    }

    /// <summary>
    /// Adds a component to the archetype builder using a struct index and stride.
    /// This is useful for components that are not known at compile time and are defined by their memory layout.
    /// </summary>
    internal ArchetypeBuilder Add(int structIndex, int stride)
    {
        var accept = new Action<IArchetypeBuilderCallback>(callback =>
        {
            callback.AcceptStrideComponent(this, structIndex, stride);
        });
        _acceptCallbacks.Add(accept);

        foreach (var filter in _filters)
        {
            accept(filter);
        }

        return this;
    }

    internal ArchetypeBuilder AddTag<T>() where T : struct, ITag
    {
        _acceptCallbacks.Add(callback =>
        {
            callback.AcceptTag<T>(this);
        });
        return this;
    }

    internal ArchetypeBuilder With(Action<ArchetypeBuilder> callback)
    {
        callback(this);
        return this;
    }

    internal ArchetypeBuilder RegisterFilter(IArchetypeBuilderCallback filter)
    {
        // NOTE: Order matters
        _filters.Add(filter);
        foreach (var accept in _acceptCallbacks.ToList())
        {
            accept(filter);
        }

        return this;
    }

    internal void Accept(IArchetypeBuilderCallback callback)
    {
        foreach (var accept in _acceptCallbacks)
        {
            accept(callback);
        }
    }

    internal void ForceComponentAOT<T>()
        where T : struct, IComponent
    {
        Add<T>();
        Add<T>(default);
    }

    internal void ForceTagAOT<T>()
        where T : struct, ITag
    {
        AddTag<T>();
    }
}