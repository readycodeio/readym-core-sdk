using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Worlds;

public class ArchetypeBuilder
{
    private readonly List<(Type, object?)> _componentTypes = [];
    private readonly List<(int StructIndex, int Stride)> _componentStrides = [];
    private readonly List<Type> _tagTypes = [];

    private readonly List<Action<IArchetypeBuilderCallback>> _acceptCallbacks = [];

    public int GetComponentCount() => _componentTypes.Count;
    public IReadOnlyList<(Type, object?)> GetComponentTypes() => _componentTypes;
    public IReadOnlyList<(int StructIndex, int Stride)> GetComponentStrides() => _componentStrides;
    public IReadOnlyList<Type> GetTagTypes() => _tagTypes;

    public ArchetypeBuilder Add<T>()
        where T : struct, IComponent
    {
        _componentTypes.Add((typeof(T), null));
        _acceptCallbacks.Add(callback =>
        {
            callback.AcceptComponentType<T>(this);
        });
        return this;
    }

    public ArchetypeBuilder Add<T>(T component)
        where T : struct, IComponent
    {
        _componentTypes.Add((typeof(T), component));
        _acceptCallbacks.Add(callback =>
        {
            callback.AcceptComponentType<T>(this, component);
        });

        return this;
    }

    public ArchetypeBuilder Add(int structIndex, int stride)
    {
        _componentStrides.Add((structIndex, stride));
        _acceptCallbacks.Add(callback =>
        {
            callback.AcceptStrideComponent(this, structIndex, stride);
        });
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

    public void Accept(IArchetypeBuilderCallback callback)
    {
        foreach (var acceptCallbacks in _acceptCallbacks)
        {
            acceptCallbacks(callback);
        }
    }
}
