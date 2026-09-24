using System.Collections.Concurrent;
using System.ComponentModel;
using Friflo.Engine.ECS;
using ReadyM.Api.DI;
using ReadyM.SDK.Entities;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Archetypes;

/// Collects [DeleteHandler]-decorated handlers that run just before a shape is deleted.
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DeleteHandlerRegistry
{
    public delegate void Handler(in EntityHandle handle);

    private static readonly ConcurrentDictionary<Type, Declaration> Handlers = new();

    public static IDependencyContainer Services => HandlerServices.Services;

    /// Called by generated code for a shape declaring a delete handler.
    public static void Register<TComponent>(Handler handler)
        where TComponent : struct, IComponent
        => Declared<TComponent>().Own = handler;

    /// Called by generated code for a service watching a shape.
    public static void Observe<TComponent>(Handler handler)
        where TComponent : struct, IComponent
        => Declared<TComponent>().Watch(handler);

    internal static bool Any => !Handlers.IsEmpty;

    /// The mirror of the create order: a watcher goes first, and the shape's own handler last.
    internal static void RunFor(in EntityHandle handle, in ComponentTypes components)
    {
        foreach (var declaration in Handlers.Values)
            if (declaration.In(components))
                declaration.RunWatching(handle);

        foreach (var declaration in Handlers.Values)
            if (declaration.In(components))
                declaration.RunOwn(handle);
    }

    /// The same, where the entity is reached by id rather than held.
    internal static void RunPresent(in EntityHandle handle, IComponentsById components)
    {
        foreach (var declaration in Handlers.Values)
            if (declaration.Present(handle, components))
                declaration.RunWatching(handle);

        foreach (var declaration in Handlers.Values)
            if (declaration.Present(handle, components))
                declaration.RunOwn(handle);
    }

    private static Declaration<TComponent> Declared<TComponent>()
        where TComponent : struct, IComponent
        => (Declaration<TComponent>)Handlers.GetOrAdd(typeof(TComponent), static _ => new Declaration<TComponent>());

    private abstract class Declaration
    {
        public abstract void RunOwn(in EntityHandle handle);

        public abstract void RunWatching(in EntityHandle handle);

        public abstract bool In(in ComponentTypes components);

        public abstract bool Present(in EntityHandle handle, IComponentsById components);
    }

    private sealed class Declaration<TComponent> : Declaration
        where TComponent : struct, IComponent
    {
#if NET
        private readonly Lock _gate = new();
#else
        private readonly object _gate = new();
#endif
        private Handler[] _watching = [];

        public Handler? Own { get; set; }

        /// Module initializers of different assemblies can land here at once.
        public void Watch(Handler handler)
        {
            lock (_gate)
                _watching = [.. _watching, handler];
        }

        public override void RunOwn(in EntityHandle handle) 
            => Own?.Invoke(handle);

        public override void RunWatching(in EntityHandle handle)
        {
            foreach (var handler in _watching)
                handler(handle);
        }

        public override bool In(in ComponentTypes components) 
            => components.Has<TComponent>();

        public override bool Present(in EntityHandle handle, IComponentsById components)
            => components.Has<TComponent>(handle.Id);
    }
}