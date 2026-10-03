using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReadyM.Api.Compat;
using ReadyM.Api.Interop.Registry;
using ReadyM.Api.Mapping.Events;

namespace ReadyM.Api.ECS.Registry;

internal class NativeTypeRegistry(IEnumerable<INativeTypeRegistration> registrations)
    : IdTypeRegistryBase<INativeTypeRegistry, ValueType, IGameEvent>(registrations), INativeTypeRegistry
{
    private readonly Dictionary<int, Type> _types = new();

    public List<Type> GetTypes()
        => _types.Values.ToList();

    public INativeTypeRegistry RegisterComponent<T>() where T : struct
    {
        AssignId<T>();
        return base.RegisterComponentImpl<T>(default);
    }

    public INativeTypeRegistry RegisterEvent<T>() where T : struct, IInteropType, IGameEvent
    {
        AssignId<T>();
        return base.RegisterEventImpl<T>();
    }

    public Type? GetTypeById(int typeId)
    {
        return _types.GetValueOrDefault(typeId);
    }

    private void AssignId<T>()
    {
        var idField = typeof(T).GetField("Id", BindingFlags.Static | BindingFlags.Public);
        var id = GetNextId();
        idField?.SetValue(null, id);
        _types[id] = typeof(T);
    }
}
