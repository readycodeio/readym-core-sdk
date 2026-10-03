using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace ReadyM.Api.ECS.Registry;

internal interface IAllTypeRegistry : ITypeRegistryBase<IAllTypeRegistry, IComponent, IGameEvent>
{
    void RegisterComponent<T>()
        where T : struct, IComponent;
    void RegisterComponent(Type componentType);

    /// <summary>Collects a mod-defined component. It has no managed type, so its full type name identifies it.</summary>
    void RegisterModComponent(ModComponentInfo info, string typeFullName);

    void RegisterEvent<T>()
        where T : struct, IGameEvent;

    /// <summary>Visits the components only; the events pass a component-only callback by.</summary>
    void Accept(IComponentRegistryCallbackBase<IAllTypeRegistry, IComponent> callback);

    /// <summary>Filters the components only; the events pass a component-only filter by.</summary>
    IAllTypeRegistry RegisterFilter(IComponentRegistryCallbackBase<IAllTypeRegistry, IComponent> filter);
}
