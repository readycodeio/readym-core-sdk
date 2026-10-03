using System;
using System.Collections.Generic;
using ReadyM.Api.Interop.Registry;

namespace ReadyM.Api.ECS.Registry;

internal interface INativeTypeRegistry : ITypeRegistryBase<INativeTypeRegistry, ValueType, IInteropType>
{
    // NOTE: Lists events too, so the native size check covers them.
    List<Type> GetComponentTypes();
    INativeTypeRegistry RegisterComponent<T>(T defaultValue = default) where T : struct;
    INativeTypeRegistry RegisterEvent<T>() where T : struct, IInteropType;
    Type? GetComponentType(int componentId);
}
