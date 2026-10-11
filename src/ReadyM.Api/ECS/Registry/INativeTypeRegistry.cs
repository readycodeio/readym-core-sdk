using System;
using System.Collections.Generic;
using ReadyM.Api.Interop.Registry;

namespace ReadyM.Api.ECS.Registry;

internal interface INativeTypeRegistry : ITypeRegistryBase<INativeTypeRegistry, ValueType, IInteropType>
{
    // NOTE: Components and events, in id order, so the native size check covers both.
    List<Type> GetTypes();
    INativeTypeRegistry RegisterComponent<T>() where T : struct;
    INativeTypeRegistry RegisterEvent<T>() where T : struct, IInteropType;
    Type? GetTypeById(int typeId);
}
