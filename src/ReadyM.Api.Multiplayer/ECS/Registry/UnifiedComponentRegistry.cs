// Copyright (c) ReadyM / ReadyCode Limited. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Friflo.Engine.ECS;
using LiteNetLib;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

/// <summary>
/// Unified registry for all ECS component types - both AOT-compiled server components and
/// runtime-registered plugin components. All components are identified by a stable <c>int</c>
/// component ID assigned at registration time.
/// <para>
/// AOT components are registered at server startup via <see cref="RegisterComponent{T}"/>.
/// Plugin components are registered at plugin load time via <see cref="RegisterPluginComponent"/>.
/// Both kinds share the same ID space and can be mixed freely in query calls.
/// </para>
/// </summary>
internal sealed class UnifiedComponentRegistry(NativeAOT aot)
{
    // -------------------------------------------------------------------------
    // Entry - one per registered component, AOT or plugin
    // -------------------------------------------------------------------------

    public sealed class Entry
    {
        public readonly int ComponentId;
        public readonly bool IsPlugin;
        public readonly Type? ManagedType; // null for plugin components
        public readonly int StructIndex; // Friflo heapMap index
        public readonly int Stride; // sizeof(T) or plugin stride
        public readonly DeliveryMethod DeliveryMethod;

        // AOT single-component fast path: uses world.Query<T>().ForEach() directly,
        // which is faster than archetype iteration because Friflo pre-filters archetypes.
        // null for plugin components; they always use the archetype-scan path.
        public readonly Action<EntityStore, ChunkCallback1>? SingleComponentQuery;

        public Entry(
            int componentId, bool isPlugin, Type? managedType,
            int structIndex, int stride, DeliveryMethod deliveryMethod,
            Action<EntityStore, ChunkCallback1>? singleComponentQuery)
        {
            ComponentId = componentId;
            IsPlugin = isPlugin;
            ManagedType = managedType;
            StructIndex = structIndex;
            Stride = stride;
            DeliveryMethod = deliveryMethod;
            SingleComponentQuery = singleComponentQuery;
        }
    }

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    // valid only during schema build phase
    private int _nextId;

    private readonly Dictionary<int, Entry> _byId = new();
    private readonly Dictionary<Type, int> _typeToId = new();
    private readonly Dictionary<string, int> _nameToId = new(); // typeof(T).FullName → id

    // -------------------------------------------------------------------------
    // Registration
    // -------------------------------------------------------------------------

    /// <summary>
    /// Registers an AOT (compile-time known) networked component. Call during server startup
    /// before <c>NativeAOT.CreateSchema()</c>.
    /// </summary>
    public int RegisterComponent<T>(DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable)
        where T : struct, IComponent
    {
        var id = _nextId++;
        var structIndex = StructInfo<T>.Index;
        var stride = Unsafe.SizeOf<T>();

        // Capture world once for the fast single-component query closure.
        Action<EntityStore, ChunkCallback1> singleQuery = (world, callback) =>
        {
            unsafe
            {
                world.Query<T>().ForEach((chunk, _) =>
                {
                    fixed (void* buffer = chunk.Span)
                    {
                        callback((IntPtr)buffer, chunk.Span.Length, stride);
                    }
                }).Run();
            }
        };

        var entry = new Entry(id, false, typeof(T), structIndex, stride, deliveryMethod, singleQuery);
        _byId[id] = entry;
        _typeToId[typeof(T)] = id;
        _nameToId[typeof(T).FullName!] = id;
        return id;
    }

    /// <summary>
    /// Registers a plugin component of unknown type with a fixed stride in bytes.
    /// Call during plugin load, before <c>NativeAOT.CreateSchema()</c>.
    /// Returns the component ID the plugin must use in all subsequent query calls.
    /// </summary>
    public int RegisterPluginComponent(int stride, DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable)
    {
        var id = _nextId++;
        var structIndex = aot.RegisterPluginComponent(stride);

        var entry = new Entry(id, true, null, structIndex, stride, deliveryMethod, null);
        _byId[id] = entry;
        return id;
    }

    // -------------------------------------------------------------------------
    // ID lookups
    // -------------------------------------------------------------------------

    public int GetId<T>() where T : struct => _typeToId[typeof(T)];
    public int GetId(Type type) => _typeToId[type];
    public int GetId(string typeFullName) => _nameToId[typeFullName];

    public DeliveryMethod GetDeliveryMethod(int id) => _byId[id].DeliveryMethod;
    public Type? GetManagedType(int id) => _byId[id].ManagedType;
    
    public Entry GetEntryById(int id) => _byId[id];
}