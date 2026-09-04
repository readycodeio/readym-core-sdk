using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Interop;
using Yooni.Native.Container;

namespace ReadyM.Relay.Server.Sdk.Ecs.Components;

internal sealed class ComponentRegistry(
    AotPointers aotPointers,
    ModComponentManager heapManager,
    ILogger logger) : IComponentRegistry
{
    private readonly RegisterModComponentDelegate _registerModComponent =
        Marshal.GetDelegateForFunctionPointer<RegisterModComponentDelegate>(aotPointers.RegisterModComponent);

    // Types this mod has registered. Only a duplicate-registration guard; it says nothing about ids, which
    // the server does not assign until every mod has finished declaring. See ModComponentIds for those.
    private readonly HashSet<Type> _declared = [];

    // One recorded action per declared component, replayed for each acceptor. Same device the native
    // registries use: it is the only way back to a typed call from a list of components.
    private readonly List<Action<IModComponentRegistryCallback>> _acceptCallbacks = [];
    private readonly List<IModComponentRegistryCallback> _filters = [];

    /// <summary>
    /// Adds an acceptor that sees every component, both the ones already declared and the ones still to come.
    /// Registering a filter before or after a given component has exactly the same effect, so a filter never
    /// has to be ordered against the mods it observes.
    /// </summary>
    internal void RegisterFilter(IModComponentRegistryCallback filter)
    {
        // NOTE: Order matters. The filter goes in first, so a component that a replayed action declares
        // reaches it too, and then everything already declared is replayed over it.
        _filters.Add(filter);
        foreach (var accept in _acceptCallbacks.ToList())
        {
            accept(filter);
        }
    }

    /// <summary>Replays every declared component for one acceptor, without adding it as a filter.</summary>
    internal void Accept(IModComponentRegistryCallback callback)
    {
        foreach (var accept in _acceptCallbacks.ToList())
        {
            accept(callback);
        }
    }

    private void Collect<T>() where T : struct
    {
        var accept = new Action<IModComponentRegistryCallback>(callback => callback.AcceptComponent<T>(this));
        _acceptCallbacks.Add(accept);

        foreach (var filter in _filters.ToList())
        {
            accept(filter);
        }
    }

    /// <summary>
    /// Declares a local component known only as a <see cref="Type"/>. One reflection hop back to the generic
    /// call, which the native registry's own by-type overload also needs: a generated nested type cannot be
    /// named from the outer type's generic argument.
    /// </summary>
    internal void RegisterLocalComponent(Type componentType)
    {
        if (!componentType.IsValueType)
            throw new ArgumentException($"{componentType.FullName} is not a value type.", nameof(componentType));

        var method = typeof(ComponentRegistry)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(m => m is { Name: nameof(RegisterLocalComponent), IsGenericMethodDefinition: true });

        method.MakeGenericMethod(componentType).Invoke(this, null);
    }

    /// <summary>
    /// Registers a mod-defined component type with the server ECS.
    /// Must be called during <c>ServerModBase.Init()</c>, before any entity creation.
    /// Returns the component ID to use in all subsequent <c>Query</c> calls.
    /// </summary>
    public void RegisterLocalComponent<T>() where T : struct
    {
        var type = typeof(T);
        var stride = Unsafe.SizeOf<T>();

        if (!_declared.Add(type))
            throw new InvalidOperationException($"{type.FullName} is already registered.");

        if (stride > 256)
            throw new ArgumentException($"{type.Name} is {stride} bytes which exceeds the 256-byte maximum.");

        var registration = heapManager.RegisterLocalComponent<T>();
        _registerModComponent(registration, new NativeString256(typeof(T).FullName, false));

        Collect<T>();
    }

    /// <summary>
    /// Registers a mod-defined component type with the server ECS.
    /// Must be called during <c>ServerModBase.Init()</c>, before any entity creation.
    /// Returns the component ID to use in all subsequent <c>Query</c> calls.
    /// </summary>
    public void RegisterComponent<T>() where T : struct, INetworkedComponent
    {
        var type = typeof(T);
        var stride = Unsafe.SizeOf<T>();

        if (!_declared.Add(type))
            throw new InvalidOperationException($"{type.FullName} is already registered.");

        if (stride > 256)
            throw new ArgumentException($"{type.Name} is {stride} bytes which exceeds the 256-byte maximum.");

        var registration = heapManager.RegisterComponent<T>();
        _registerModComponent(registration, new NativeString256(typeof(T).FullName, false));

        logger.LogDebug("Registered component {Component}", type.FullName);

        // The generated ChangeComponent that goes with this one is derived by a filter, see
        // ModChangeComponentRegistration, rather than dug out of the type here.
        Collect<T>();
    }
}
