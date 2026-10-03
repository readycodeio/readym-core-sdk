using System.Collections.Generic;

namespace ReadyM.Api.ECS.Registry;

// NOTE: A component-only registry. The implementation is in `TypeRegistryBase`; this only keeps the narrow interfaces.
internal abstract class ComponentRegistryBase<TRegistry, TComponent>(
    IEnumerable<IComponentRegistrationBase<TRegistry, TComponent>> registrations)
    : TypeRegistryBase<TRegistry, TComponent, INoGameEvents>(registrations), IComponentRegistryBase<TRegistry, TComponent>
    where TRegistry : IComponentRegistryBase<TRegistry, TComponent>
{
    public TRegistry RegisterFilter(IComponentRegistryCallbackBase<TRegistry, TComponent> filter)
        => RegisterFilter(new ComponentOnlyCallback<TRegistry, TComponent>(filter));

    public void Accept(IComponentRegistryCallbackBase<TRegistry, TComponent> callback)
        => Accept(new ComponentOnlyCallback<TRegistry, TComponent>(callback));
}
