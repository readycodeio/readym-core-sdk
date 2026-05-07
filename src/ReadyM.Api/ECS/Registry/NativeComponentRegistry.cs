using System;
using System.Collections.Generic;
using System.Reflection;

namespace ReadyM.Api.ECS.Registry;

public class NativeComponentRegistry(IEnumerable<INativeComponentRegistration> registrations)
    : ComponentRegistryBase<INativeComponentRegistry, ValueType>(registrations), INativeComponentRegistry
{
    public override INativeComponentRegistry RegisterComponent<T>(T defaultValue = default)
    {
        var idField = typeof(T).GetField("Id", BindingFlags.Static | BindingFlags.Public);
        idField?.SetValue(null, GetNextComponentId());
        return base.RegisterComponent(defaultValue);
    }
}
