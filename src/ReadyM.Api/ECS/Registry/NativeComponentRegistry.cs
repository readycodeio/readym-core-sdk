using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReadyM.Api.Compat;

namespace ReadyM.Api.ECS.Registry;

internal class NativeComponentRegistry(IEnumerable<INativeComponentRegistration> registrations)
    : ComponentRegistryBase<INativeComponentRegistry, ValueType>(registrations), INativeComponentRegistry
{
    private readonly Dictionary<int, Type> _componentTypes = new();

    public List<Type> ComponentTypes => _componentTypes.Values.ToList();

    public override INativeComponentRegistry RegisterComponent<T>(T defaultValue = default) where T : struct
    {
        var idField = typeof(T).GetField("Id", BindingFlags.Static | BindingFlags.Public);
        var id = GetNextComponentId();
        idField?.SetValue(null, id);
        _componentTypes[id] = typeof(T);
        return base.RegisterComponent(defaultValue);
    }

    public Type? GetComponentType(int componentId)
    {
        return _componentTypes.GetValueOrDefault(componentId);
    }
}