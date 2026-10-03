using System;
using System.Collections.Generic;
using ReadyM.Api.Interop.Registry;
using ReadyM.Api.Mapping.Events;

namespace ReadyM.Api.ECS.Registry;

internal interface INativeTypeRegistry : ITypeRegistryBase<INativeTypeRegistry, ValueType, IGameEvent>
{
    // NOTE: Components and events, in id order, so the native size check covers both.
    List<Type> GetTypes();
    INativeTypeRegistry RegisterComponent<T>() where T : struct;
    INativeTypeRegistry RegisterEvent<T>() where T : struct, IInteropType, IGameEvent;
    Type? GetTypeById(int typeId);
}
