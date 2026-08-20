using System;
using System.Collections.Generic;

namespace ReadyM.Api.ECS.Registry;

internal interface INativeComponentRegistry : IComponentRegistryBase<INativeComponentRegistry, ValueType>
{
    List<Type> GetComponentTypes();
    INativeComponentRegistry RegisterComponent<T>(T defaultValue = default) where T : struct;
    Type? GetComponentType(int componentId);
}
