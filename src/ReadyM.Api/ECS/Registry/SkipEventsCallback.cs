using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Registry;

// NOTE: Lets a component-only callback visit a registry that also holds events, which pass it by.
internal sealed class SkipEventsCallback<TRegistry, TComponent, TEvent>(IComponentRegistryCallbackBase<TRegistry, TComponent> inner)
    : ITypeRegistryCallbackBase<TRegistry, TComponent, TEvent>
{
    public void AcceptComponent<T>(TRegistry registry, T defaultValue = default)
        where T : struct, TComponent
        => inner.AcceptComponent(registry, defaultValue);

    public void AcceptModComponent(TRegistry registry, ModComponentInfo info, string typeFullName)
        => inner.AcceptModComponent(registry, info, typeFullName);

    public void AcceptEvent<T>(TRegistry registry)
        where T : struct, TEvent
    {
        // Events are not components.
    }
}
