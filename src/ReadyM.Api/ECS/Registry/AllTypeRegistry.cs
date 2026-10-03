using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using ReadyM.Api.Mapping.Events;

namespace ReadyM.Api.ECS.Registry;

/// <summary>
/// The list of every ECS component and game event this side knows about, whether compiled in or defined by a mod.
/// <para>
/// Gathering that list is all it does. The registrations it is built from call
/// <see cref="RegisterComponent{T}()"/>, <see cref="RegisterModComponent"/> and <see cref="RegisterEvent{T}"/>, which
/// record that a type exists and nothing more. When the constructor returns the registry seals, and a later
/// registration throws: anything holding a finished registry is entitled to assume the set is complete.
/// </para>
/// <para>
/// Component ids, struct indices and the interop delegates live in <c>UnifiedComponentRegistry</c>, which is
/// built from this list once the schema exists. They are deliberately not here: none of them can be known
/// while this is still being filled in.
/// </para>
/// </summary>
internal class AllTypeRegistry(IEnumerable<IAllTypeRegistration> registrations)
    : TypeRegistryBase<IAllTypeRegistry, IComponent, IGameEvent>(registrations), IAllTypeRegistry
{
    public void RegisterComponent<T>() where T : struct, IComponent
        => RegisterComponentImpl(default(T));

    public void RegisterComponent(Type componentType)
        => RegisterComponentImpl(componentType);

    public void RegisterModComponent(ModComponentInfo info, string typeFullName)
        => RegisterModComponentImpl(info, typeFullName);

    public void RegisterEvent<T>() where T : struct, IGameEvent
        => RegisterEventImpl<T>();

    public void Accept(IComponentRegistryCallbackBase<IAllTypeRegistry, IComponent> callback)
        => Accept(new SkipEventsCallback<IAllTypeRegistry, IComponent, IGameEvent>(callback));

    public IAllTypeRegistry RegisterFilter(IComponentRegistryCallbackBase<IAllTypeRegistry, IComponent> filter)
        => RegisterFilter(new SkipEventsCallback<IAllTypeRegistry, IComponent, IGameEvent>(filter));

    internal void ForceAOT<T>()
        where T : struct, IComponent
        => RegisterComponent<T>();
}
