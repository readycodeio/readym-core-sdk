using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Registry;

internal interface ITypeRegistryCallbackBase<in TRegistry, in TComponent, in TEvent>
{
    void AcceptComponent<T>(TRegistry registry, T defaultValue = default)
        where T : struct, TComponent;

    /// <summary>
    /// A component defined by a mod, identified by its full type name; see
    /// <see cref="IComponentRegistryCallbackBase{TRegistry,TComponent}.AcceptModComponent"/>.
    /// </summary>
    void AcceptModComponent(TRegistry registry, ModComponentInfo info, string typeFullName);

    void AcceptEvent<T>(TRegistry registry)
        where T : struct, TEvent;
}
