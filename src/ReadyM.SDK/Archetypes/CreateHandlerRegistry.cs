using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using Friflo.Engine.ECS;
using ReadyM.Api.DI;
using ReadyM.SDK.Entities;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Archetypes;

/// Collects [CreateHandler]-decorated handlers that run when a shape is created.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CreateHandlerRegistry
{
    public delegate void Handler(in EntityHandle handle);

    private static readonly ConcurrentDictionary<Type, Declaration> Handlers = new();

    private static IDependencyContainer? _services;

    /// Call this once when the game initializes to wire up the DI for the handlers.
    public static void Use(IDependencyContainer services) => _services = services;

    public static IDependencyContainer Services
        => _services ?? throw new InvalidOperationException(
            "A create handler asked for a service before the game registered the SDK with the SDK's "
            + "own container. Call RegisterReadyMSdk on it first.");

    /// Called by generated code for a shape declaring a create handler.
    public static void Register<TComponent>(Handler handler)
        where TComponent : struct, IComponent
        => Declared<TComponent>().Own = handler;

    /// Called by generated code for a service watching a shape.
    public static void Observe<TComponent>(Handler handler)
        where TComponent : struct, IComponent
        => Declared<TComponent>().Watch(handler);

    internal static bool Any => !Handlers.IsEmpty;

    internal static void RunAll(in EntityHandle handle, ComponentSet components)
    {
        foreach (var component in components.Types)
            if (Handlers.TryGetValue(component, out var declaration))
                declaration.RunOwn(handle);

        foreach (var component in components.Types)
            if (Handlers.TryGetValue(component, out var declaration))
                declaration.RunWatching(handle);
    }

    internal static void RunPresent(in EntityHandle handle, IComponentsById components)
    {
        foreach (var declaration in Handlers.Values)
            if (declaration.Present(handle, components))
                declaration.RunOwn(handle);

        foreach (var declaration in Handlers.Values)
            if (declaration.Present(handle, components))
                declaration.RunWatching(handle);
    }

    private static Declaration<TComponent> Declared<TComponent>()
        where TComponent : struct, IComponent
        => (Declaration<TComponent>)Handlers.GetOrAdd(typeof(TComponent), static _ => new Declaration<TComponent>());

    private abstract class Declaration
    {
        public abstract void RunOwn(in EntityHandle handle);

        public abstract void RunWatching(in EntityHandle handle);

        public abstract bool Present(in EntityHandle handle, IComponentsById components);
    }

    private sealed class Declaration<TComponent> : Declaration
        where TComponent : struct, IComponent
    {
        private readonly object _gate = new();

        private Handler[] _watching = [];

        public Handler? Own { get; set; }

        /// Module initializers of different assemblies can land here at once.
        public void Watch(Handler handler)
        {
            lock (_gate)
                _watching = [.. _watching, handler];
        }

        public override void RunOwn(in EntityHandle handle) => Own?.Invoke(handle);

        public override void RunWatching(in EntityHandle handle)
        {
            foreach (var handler in _watching)
                handler(handle);
        }

        public override bool Present(in EntityHandle handle, IComponentsById components)
            => components.Has<TComponent>(handle.Id);
    }
}