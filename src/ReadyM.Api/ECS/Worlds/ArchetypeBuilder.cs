using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Worlds;

public class ArchetypeBuilder
{
    private readonly List<(Type, object?)> _componentTypes = [];
    private readonly List<(int StructIndex, int Stride)> _componentStrides = [];
    private readonly List<Type> _tagTypes = [];

    private readonly List<Action<IArchetypeBuilderCallback>> _acceptCallbacks = [];
    private readonly List<IArchetypeBuilderCallback> _filters = [];

    public int GetComponentCount() => _componentTypes.Count;
    public IReadOnlyList<(Type, object?)> GetComponentTypes() => _componentTypes;
    public IReadOnlyList<(int StructIndex, int Stride)> GetComponentStrides() => _componentStrides;
    public IReadOnlyList<Type> GetTagTypes() => _tagTypes;

    public ArchetypeBuilder Add(Type componentType)
    {
        if (!componentType.IsValueType)
            throw new ArgumentException($"Type {componentType} is not a value type.", nameof(componentType));
        if (!typeof(IComponent).IsAssignableFrom(componentType))
            throw new ArgumentException($"Type {componentType} does not implement IComponent.", nameof(componentType));

        var method = typeof(ArchetypeBuilder).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(m => m.Name == nameof(Add) && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
        method = method.MakeGenericMethod(componentType);

        method.Invoke(this, null);
        return this;
    }

    public ArchetypeBuilder Add<T>()
        where T : struct, IComponent
    {
        _componentTypes.Add((typeof(T), null));

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

    public ArchetypeBuilder Add<T>(T component)
        where T : struct, IComponent
    {
        _componentTypes.Add((typeof(T), component));

        var accept = new Action<IArchetypeBuilderCallback>(callback =>
        {
            callback.AcceptComponentType<T>(this, component);
        });
        _acceptCallbacks.Add(accept);

        foreach (var filter in _filters)
        {
            accept(filter);
        }

        return this;
    }

    public ArchetypeBuilder Add(int structIndex, int stride)
    {
        _componentStrides.Add((structIndex, stride));

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

    public ArchetypeBuilder AddTag<T>()
        where T : struct, ITag
    {
        _tagTypes.Add(typeof(T));
        _acceptCallbacks.Add(callback =>
        {
            callback.AcceptTag<T>(this);
        });
        return this;
    }

    public ArchetypeBuilder With(Action<ArchetypeBuilder> callback)
    {
        callback(this);
        return this;
    }

    public ArchetypeBuilder RegisterFilter(IArchetypeBuilderCallback filter)
    {
        // NOTE: Order matters
        _filters.Add(filter);
        foreach (var accept in _acceptCallbacks.ToList())
        {
            accept(filter);
        }

        return this;
    }

    public void Accept(IArchetypeBuilderCallback callback)
    {
        foreach (var accept in _acceptCallbacks)
        {
            accept(callback);
        }
    }
}
