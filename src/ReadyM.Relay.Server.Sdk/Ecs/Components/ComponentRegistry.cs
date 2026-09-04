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

    private readonly GetComponentIdByNameDelegate _getComponentIdByName =
        Marshal.GetDelegateForFunctionPointer<GetComponentIdByNameDelegate>(aotPointers.GetComponentIdByName);

    // Component ids resolved from the server, cached per type. Filled on first use, not at registration:
    // the server only knows a mod component's id once it has run its acceptor pass, which happens after the
    // mod has finished registering.
    private readonly Dictionary<Type, int> _resolvedIds = new();

    // Types this mod has registered. Only a duplicate-registration guard; it says nothing about ids.
    private readonly HashSet<Type> _declared = [];

    /// <summary>
    /// The id the server assigned to a component, resolved by full type name on first use and cached.
    /// <para>
    /// This is the only path, including for components this mod registered itself. Registration merely tells
    /// the server the component exists; the id is decided later, when the server registers everything into
    /// the schema and reads the result back. So asking earlier than the first query would be asking before
    /// there is an answer.
    /// </para>
    /// </summary>
    internal int ResolveComponentId<T>() where T : struct
    {
        if (_resolvedIds.TryGetValue(typeof(T), out var cached))
            return cached;

        var id = _getComponentIdByName(new NativeString256(typeof(T).FullName, false));
        if (id < 0)
        {
            throw new InvalidOperationException(
                $"The server does not know component {typeof(T).FullName}. Either it was never registered, or "
                + "this ran before the server finished building its component table.");
        }

        _resolvedIds.Add(typeof(T), id);
        return id;
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

        var nestedTypeName = "ChangeComponent";
        var nestedType = type.GetNestedType(nestedTypeName, BindingFlags.Public);

        if (nestedType != null)
        {
            // call RegisterLocalComponent<ChangeComponent>() for the nested type
            var registerMethod = typeof(ComponentRegistry).GetMethod(nameof(RegisterLocalComponent), BindingFlags.Public | BindingFlags.Instance);
            var genericMethod = registerMethod!.MakeGenericMethod(nestedType);
            genericMethod.Invoke(this, null);
        }
    }
}
