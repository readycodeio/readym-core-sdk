using System.Collections.Concurrent;
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

    // Maps mod struct type → component ID assigned by the server registry. Registration happens once
    // at startup; resolution happens per component access, so the lookup is kept thread safe. Asking
    // the server for the same name twice returns the same id, so a racing miss costs one extra call.
    private readonly ConcurrentDictionary<Type, (int ComponentId, int Stride)> _registered = new();

    internal int ResolveComponentId<T>() where T : struct => ResolveComponentId(typeof(T));

    internal int ResolveComponentId(Type type) => _registered.GetOrAdd(type, static (key, self)
        => (self._getComponentIdByName(new NativeString256(key.FullName, false)), -1), this).ComponentId;

    /// <summary>
    /// Registers a mod-defined component type with the server ECS.
    /// Must be called during <c>ServerModBase.Init()</c>, before any entity creation.
    /// Returns the component ID to use in all subsequent <c>Query</c> calls.
    /// </summary>
    public void RegisterLocalComponent<T>() where T : struct
    {
        var type = typeof(T);
        var stride = Unsafe.SizeOf<T>();

        if (_registered.ContainsKey(type))
            throw new InvalidOperationException($"{type.FullName} is already registered.");

        if (stride > 256)
            throw new ArgumentException($"{type.Name} is {stride} bytes which exceeds the 256-byte maximum.");

        var registration = heapManager.RegisterLocalComponent<T>();
        var id = _registerModComponent(registration, new NativeString256(typeof(T).FullName, false));

        if (id < 0)
            throw new InvalidOperationException(
                $"Server refused to register {type.Name}: component slot limit reached.");

        _registered[type] = (id, stride);
    }

    /// <summary>
    /// Registers a mod-defined component type with the server ECS.
    /// Must be called during <c>ServerModBase.Init()</c>, before any entity creation.
    /// Returns the component ID to use in all subsequent <c>Query</c> calls.
    /// </summary>
    /// Reaches the generic overload for a type only known at run time. The constraint cannot be
    /// checked here, so a type that does not satisfy it is refused rather than left to fail inside.
    public void RegisterComponent(Type component, byte delivery)
    {
        if (!typeof(INetworkedComponent).IsAssignableFrom(component))
            throw new ArgumentException(
                $"{component.FullName} does not replicate, so it cannot be registered as networked.",
                nameof(component));

        var method = typeof(ComponentRegistry)
            .GetMethod(nameof(RegisterComponent), BindingFlags.Public | BindingFlags.Instance, [typeof(byte)]);

        method!.MakeGenericMethod(component).Invoke(this, [delivery]);
    }

    public void RegisterComponent<T>(byte delivery = 0) where T : struct, INetworkedComponent
    {
        var type = typeof(T);
        var stride = Unsafe.SizeOf<T>();

        if (_registered.ContainsKey(type))
            throw new InvalidOperationException($"{type.FullName} is already registered.");

        if (stride > 256)
            throw new ArgumentException($"{type.Name} is {stride} bytes which exceeds the 256-byte maximum.");

        var registration = heapManager.RegisterComponent<T>(delivery);
        var id = _registerModComponent(registration, new NativeString256(typeof(T).FullName, false));

        if (id < 0)
            throw new InvalidOperationException($"Server refused to register {type.Name}: component slot limit reached.");

        logger.LogDebug("Registered component {Component} with ID {Id}", type.FullName, id);

        _registered[type] = (id, stride);

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
