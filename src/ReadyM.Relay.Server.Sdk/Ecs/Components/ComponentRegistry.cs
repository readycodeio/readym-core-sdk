using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using ReadyM.Api.Multiplayer.ECS.Archetypes;
using Yooni.Native.LowLevel;
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

    // Absent in a host with no archetypes of its own to extend, such as the test harness.
    private readonly AddArchetypeExtensionsDelegate? _addArchetypeExtensions =
        aotPointers.AddArchetypeExtensions == IntPtr.Zero
            ? null
            : Marshal.GetDelegateForFunctionPointer<AddArchetypeExtensionsDelegate>(aotPointers.AddArchetypeExtensions);

    // Maps mod struct type → component ID assigned by the server registry. Registration happens once
    // at startup; resolution happens per component access, so the lookup is kept thread safe. Asking
    // the server for the same name twice returns the same id, so a racing miss costs one extra call.
    private readonly ConcurrentDictionary<Type, (int ComponentId, int Stride)> _registered = new();

    /// The generic overload of a name, chosen by its arity and parameters rather than by name
    /// alone. Picking by name breaks the moment the name gains another overload, which is how a
    /// mod's whole component registration once failed on an AmbiguousMatchException.
    private static MethodInfo GenericMethod(string name, Type[] parameters)
        => typeof(ComponentRegistry).GetMethod(
               name, 1, BindingFlags.Public | BindingFlags.Instance, null, parameters, null)
           ?? throw new InvalidOperationException($"No generic {name} with {parameters.Length} parameter(s).");

    internal int ResolveComponentId<T>() where T : struct => ResolveComponentId(typeof(T));

    internal int ResolveComponentId(Type type)
    {
        var id = _registered.GetOrAdd(type, static (key, self)
            => (self._getComponentIdByName(new NativeString256(key.FullName, false)), -1), this).ComponentId;

        if (id < 0)
        {
            // Not cached: a component registered later would otherwise stay unknown forever.
            _registered.TryRemove(type, out _);

            throw new InvalidOperationException(
                $"The host does not know {type.FullName}. A component reaches it when the mod that "
                + "declares it registers its components.");
        }

        return id;
    }

    /// The same as <see cref="RegisterLocalComponent{T}"/> for a type only known at run time.
    public void RegisterLocalComponent(Type component)
    {
        GenericMethod(nameof(RegisterLocalComponent), Type.EmptyTypes)
            .MakeGenericMethod(component)
            .Invoke(this, null);
    }
    
    /// Adds components to an archetype the game registered, named by the shape it is declared as
    /// so that a mod never holds an archetype id. Belongs in this phase: the host builds those
    /// archetypes as its schema is created, once every mod has registered its components.
    public void AddArchetypeExtensions(Type shape, IReadOnlyList<Type> components)
    {
        var ids = new List<int>(components.Count);

        foreach (var component in components)
        {
            try
            {
                ids.Add(ResolveComponentId(component));
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"{component.FullName} was added to {shape.FullName}, but the host does not "
                    + "know it. A component reaches the host when the mod that declares it "
                    + "registers its components, which happens before this.", e);
            }
        }

        if (ids.Count == 0 || _addArchetypeExtensions is null)
            return;

        logger.LogDebug("Adding {Components} to {Shape}", ids, shape.FullName);

        var native = new NativeList<int>(ids.Count, AllocatorKind.Default);

        foreach (var id in ids)
            native.Add(id);

        _addArchetypeExtensions(new NativeString256(shape.FullName, false), native);
    }

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
        
        if (_registered.ContainsKey(component))
            return;

        GenericMethod(nameof(RegisterComponent), [typeof(byte)])
            .MakeGenericMethod(component)
            .Invoke(this, [delivery]);
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
            var genericMethod = GenericMethod(nameof(RegisterLocalComponent), Type.EmptyTypes)
                .MakeGenericMethod(nestedType);
            genericMethod.Invoke(this, null);
        }
    }
}
