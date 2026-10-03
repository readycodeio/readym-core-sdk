using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReadyM.Api.Compat;
using ReadyM.Api.Interop.Registry;

namespace ReadyM.Api.ECS.Registry;

internal class NativeTypeRegistry(IEnumerable<INativeTypeRegistration> registrations)
    : IdTypeRegistryBase<INativeTypeRegistry, ValueType, IInteropType>(registrations), INativeTypeRegistry
{
    private readonly Dictionary<int, Type> _componentTypes = new();

    public List<Type> GetComponentTypes()
        => _componentTypes.Values.ToList();

    public INativeTypeRegistry RegisterComponent<T>(T defaultValue = default) where T : struct
    {
        AssignId<T>();
        return base.RegisterComponentImpl(defaultValue);
    }

    public INativeTypeRegistry RegisterEvent<T>() where T : struct, IInteropType
    {
        AssignId<T>();
        return base.RegisterEventImpl<T>();
    }

    public Type? GetComponentType(int componentId)
    {
        return _componentTypes.GetValueOrDefault(componentId);
    }

    private void AssignId<T>()
    {
        var idField = typeof(T).GetField("Id", BindingFlags.Static | BindingFlags.Public);
        var id = GetNextId();
        idField?.SetValue(null, id);
        _componentTypes[id] = typeof(T);
    }
}
