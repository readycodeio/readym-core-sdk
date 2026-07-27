using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Interop;
using Yooni.Native.Container;

namespace ReadyM.Relay.Server.Sdk.Ecs.Components;

internal sealed class ComponentRegistry(AotPointers aotPointers, ModComponentManager heapManager, ILogger logger) : IComponentRegistry
{
    private readonly RegisterModComponentDelegate registerModComponent =
        Marshal.GetDelegateForFunctionPointer<RegisterModComponentDelegate>(aotPointers.RegisterModComponent);

    private readonly GetComponentIdByNameDelegate _getComponentIdByName =
        Marshal.GetDelegateForFunctionPointer<GetComponentIdByNameDelegate>(aotPointers.GetComponentIdByName);

    // Maps mod struct type → component ID assigned by the server registry.
    private readonly Dictionary<Type, (int ComponentId, int Stride)> _registered = new();

    internal int ResolveComponentId<T>() where T : struct
    {
        if (_registered.TryGetValue(typeof(T), out var entry))
        {
            // found locally
            return entry.ComponentId;
        }

        var id = _getComponentIdByName(new NativeString256(typeof(T).FullName, false));
        _registered.Add(typeof(T), (id, -1));

        return id;
    }

    /// <summary>
    /// Registers a mod-defined component type with the server ECS.
    /// Must be called during <c>ServerModBase.Init()</c>, before any entity creation.
    /// Returns the component ID to use in all subsequent <c>Query</c> calls.
    /// </summary>
    public int RegisterLocalComponent<T>() where T : struct
    {
        var type = typeof(T);
        var stride = Unsafe.SizeOf<T>();

        if (_registered.ContainsKey(type))
            throw new InvalidOperationException($"{type.FullName} is already registered.");

        if (stride > 256)
            throw new ArgumentException($"{type.Name} is {stride} bytes which exceeds the 256-byte maximum.");

        var registration = heapManager.RegisterLocalComponent<T>();
        var id = registerModComponent(registration, new NativeString256(typeof(T).Name, false));

        if (id < 0)
            throw new InvalidOperationException(
                $"Server refused to register {type.Name}: component slot limit reached.");

        _registered[type] = (id, stride);
        return id;
    }
    
    /// <summary>
    /// Registers a mod-defined component type with the server ECS.
    /// Must be called during <c>ServerModBase.Init()</c>, before any entity creation.
    /// Returns the component ID to use in all subsequent <c>Query</c> calls.
    /// </summary>
    public int RegisterComponent<T>() where T : struct, INetworkedComponent
    {
        var type = typeof(T);
        var stride = Unsafe.SizeOf<T>();

        if (_registered.ContainsKey(type))
            throw new InvalidOperationException($"{type.FullName} is already registered.");

        if (stride > 256)
            throw new ArgumentException($"{type.Name} is {stride} bytes which exceeds the 256-byte maximum.");

        var registration = heapManager.RegisterComponent<T>();
        var id = registerModComponent(registration, new NativeString256(typeof(T).Name, false));

        if (id < 0)
            throw new InvalidOperationException($"Server refused to register {type.Name}: component slot limit reached.");

        logger.LogDebug("Registered component {Component} with ID {Id}", type.FullName, id);

        _registered[type] = (id, stride);
        return id;
    }
}