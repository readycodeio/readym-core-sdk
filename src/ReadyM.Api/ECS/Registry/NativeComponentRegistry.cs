using System;
using System.Collections.Generic;

namespace ReadyM.Api.ECS.Registry;

public class NativeComponentRegistry(IEnumerable<INativeComponentRegistration> registrations)
    : ComponentRegistryBase<INativeComponentRegistry, ValueType>(registrations), INativeComponentRegistry
{
    // empty
}