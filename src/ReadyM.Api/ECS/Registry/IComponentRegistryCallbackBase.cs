using Friflo.Engine.ECS;

namespace ReadyM.Api.ECS.Registry;

internal interface IComponentRegistryCallbackBase<in TRegistry, in TComponent>
{
    /// <summary>
    /// A component compiled into this build. Its managed type is its identity, so <typeparamref name="T"/>
    /// carries everything an acceptor needs and the registered default value travels with it.
    /// <para>
    /// Every acceptor implements this one. A registry that carries components at all carries native ones.
    /// </para>
    /// </summary>
    void AcceptComponent<T>(TRegistry registry, T defaultValue = default)
        where T : struct, TComponent;

    /// <summary>
    /// A component defined by a mod. It has no managed type on this side, so it arrives as a stride plus a
    /// set of function pointers, identified by its full type name.
    /// <para>
    /// Deliberately has no default implementation. Whether a registry can carry mod components is a decision
    /// per registry, and a default would let a new one inherit an answer nobody chose. A registry that cannot
    /// should throw <see cref="System.NotSupportedException"/> and say why.
    /// </para>
    /// </summary>
    void AcceptModComponent(TRegistry registry, ModComponentInfo info, string typeFullName);
}
