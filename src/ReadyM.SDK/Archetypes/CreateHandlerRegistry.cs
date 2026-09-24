using System.Collections.Concurrent;
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

    private static readonly ConcurrentDictionary<Type, Handler> Handlers = new();

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
        => Handlers[typeof(TComponent)] = handler;

    internal static bool Any => !Handlers.IsEmpty;

    internal static void RunAll(in EntityHandle handle, ComponentSet components)
    {
        foreach (var component in components.Types)
            if (Handlers.TryGetValue(component, out var handler))
                handler(handle);
    }
}
