 using System;
using System.Diagnostics.CodeAnalysis;
using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Registry;

internal interface IArchetypeComponentRegistryBase<out TRegistry> : IComponentRegistryBase<TRegistry, IComponent>
    where TRegistry : IComponentRegistryBase<TRegistry, IComponent>
{
    void RegisterComponent<T>(Func<T>? valueFactory = null)
        where T : struct, IComponent;

    bool TryGetValueFactory<T>([NotNullWhen(true)] out Func<T>? valueFactory)
        where T : struct, IComponent;
}
