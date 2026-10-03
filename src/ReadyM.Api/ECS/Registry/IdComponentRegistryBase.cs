using System.Collections.Generic;

namespace ReadyM.Api.ECS.Registry;

// NOTE: A component-only registry with ids. The counter is in `IdTypeRegistryBase`; this only keeps the narrow
// interfaces.
internal abstract class IdComponentRegistryBase<TRegistry, TComponent>(
    IEnumerable<IComponentRegistrationBase<TRegistry, TComponent>> registrations)
    : IdTypeRegistryBase<TRegistry, TComponent, INoGameEvents>(registrations), IComponentRegistryBase<TRegistry, TComponent>
    where TRegistry : IComponentRegistryBase<TRegistry, TComponent>
{
    protected byte GetNextComponentId()
        => GetNextId();

    public TRegistry RegisterFilter(IComponentRegistryCallbackBase<TRegistry, TComponent> filter)
        => RegisterFilter(new ComponentOnlyCallback<TRegistry, TComponent>(filter));

    public void Accept(IComponentRegistryCallbackBase<TRegistry, TComponent> callback)
        => Accept(new ComponentOnlyCallback<TRegistry, TComponent>(callback));
}
