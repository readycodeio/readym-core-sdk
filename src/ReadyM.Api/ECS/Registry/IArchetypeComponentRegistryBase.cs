 using System;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Registry;

internal interface IArchetypeComponentRegistryBase<out TRegistry> : IComponentRegistryBase<TRegistry, IComponent>
    where TRegistry : IComponentRegistryBase<TRegistry, IComponent>
{
    void RegisterComponent<T>(T defaultValue = default)
        where T : struct, IComponent;
}
