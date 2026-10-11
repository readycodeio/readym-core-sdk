using System;
using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Registry;

// NOTE: Lets a component-only registry take its narrow callbacks. `AcceptEvent` is unreachable: nothing implements
// `INoGameEvents`, so no event can be registered.
internal sealed class ComponentOnlyCallback<TRegistry, TComponent>(IComponentRegistryCallbackBase<TRegistry, TComponent> inner)
    : ITypeRegistryCallbackBase<TRegistry, TComponent, INoGameEvents>
{
    public void AcceptComponent<T>(TRegistry registry, T defaultValue = default)
        where T : struct, TComponent
        => inner.AcceptComponent(registry, defaultValue);

    public void AcceptModComponent(TRegistry registry, ModComponentInfo info, string typeFullName)
        => inner.AcceptModComponent(registry, info, typeFullName);

    public void AcceptEvent<T>(TRegistry registry)
        where T : struct, INoGameEvents
        => throw new NotSupportedException($"Registry {typeof(TRegistry).Name} holds no game events, but was asked to accept {typeof(T).Name}");
}
