using System.Collections.Concurrent;
using System.ComponentModel;
using Friflo.Engine.ECS;
using ReadyM.Api.ECS.Components;
using ReadyM.SDK.Entities;
using Yooni.Native.LowLevel;

namespace ReadyM.SDK.Archetypes;

/// Which components have to be given their memory before anything reads them.
/// <remarks>
/// A native collection is a handle to memory nobody has taken yet, so a component holding one is
/// unusable until it has been created. Creating an entity runs this for every component it carries,
/// which is why a mod never has to remember to.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class NativeInitRegistry
{
    private static readonly ConcurrentDictionary<Type, Initializer> Initializers = new();

    /// Called by generated code for a component holding a native collection.
    public static void Register<TComponent>()
        where TComponent : struct, Friflo.Engine.ECS.IComponent, INativeInit
        => Initializers[typeof(TComponent)] = new Initializer<TComponent>();

    /// Nothing to do for the common shape, which holds no collection at all, so the lookup is
    /// skipped outright rather than once per component.
    internal static bool Any => !Initializers.IsEmpty;

    internal static void InitAll(in EntityHandle handle, ComponentSet components)
    {
        foreach (var component in components.Types)
            if (Initializers.TryGetValue(component, out var initializer))
                initializer.Init(handle);
    }

    // The component type is only known where the shape was generated, so it is captured here. The
    // call reaches the component by reference, so Init writes into the store rather than a copy.
    private abstract class Initializer
    {
        internal abstract void Init(in EntityHandle handle);
    }

    private sealed class Initializer<T> : Initializer
        where T : struct, Friflo.Engine.ECS.IComponent, INativeInit
    {
        internal override void Init(in EntityHandle handle)
            => handle.GetComponent<T>().Init(AllocatorKind.Default);
    }
}
