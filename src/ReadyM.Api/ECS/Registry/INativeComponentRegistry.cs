using System;

namespace ReadyM.Api.ECS.Registry;

internal interface INativeComponentRegistry : IComponentRegistryBase<INativeComponentRegistry, ValueType>
{
    INativeComponentRegistry RegisterComponent<T>(T defaultValue = default) where T : struct;
    Type? GetComponentType(int componentId);
}
