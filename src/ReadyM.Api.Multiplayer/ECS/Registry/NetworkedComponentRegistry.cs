using System;
using System.Collections.Generic;
using Friflo.Engine.ECS;
using LiteNetLib;
using ReadyM.Api.ECS.Registry;
using ReadyM.Api.Multiplayer.ECS.Components;
using ReadyM.Api.Multiplayer.Interop;

namespace ReadyM.Api.Multiplayer.ECS.Registry;

internal class NetworkedComponentRegistry(EntityStore world, IEnumerable<INetworkedComponentRegistration> registrations)
    : ComponentRegistryBase<INetworkedComponentRegistry, INetworkedComponent>(registrations), INetworkedComponentRegistry
{
    private struct AotEcsCallbacks
    {
        public Type ComponentType;
        public ComponentTypes ComponentTypes;
        public HostQueryDelegate1 Query1;
    }

    private byte _nextComponentId;
    private readonly Dictionary<string, (NetworkedComponentId Id, DeliveryMethod DeliveryMethod)> _componentIds = new();
    private readonly Dictionary<NetworkedComponentId, Type> _componentTypes = new();
    private readonly Dictionary<NetworkedComponentId, AotEcsCallbacks> _aotEcsCallbacks = new();

    public new INetworkedComponentRegistry RegisterComponent<T>(T defaultValue = default)
        where T : struct, INetworkedComponent
        => RegisterComponent(DeliveryMethod.Unreliable, defaultValue);

    public INetworkedComponentRegistry RegisterComponent<T>(DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable, T defaultValue = default)
        where T : struct, INetworkedComponent
    {
        var id = new NetworkedComponentId(_nextComponentId++);
        _componentIds.Add(typeof(T).FullName!, (id, deliveryMethod));
        _componentTypes.Add(id, typeof(T));

        _aotEcsCallbacks.Add(id, new AotEcsCallbacks
        {
            ComponentType = typeof(T),
            ComponentTypes = Signature.Get<T>().ComponentTypes,
            Query1 = RunQuery<T>
        });
        return base.RegisterComponent(defaultValue);
    }

    public NetworkedComponentId GetNetworkedComponentId(Type type)
        => _componentIds[type.FullName].Id;

    private unsafe void RunQuery<T>(EmbedQueryDelegate1 callback) where T : struct, IComponent
    {
        // ptr is a pointer to an unmanaged function that takes a pointer to an array of T and an int for the length of the array
        // and casts it to Span<T> internally, then runs some void method for all elements of the span
        world.Query<T>().ForEach((chunk, _) =>
        {
            fixed (void* buffer = chunk.Span)
            {
                var chunks = new Chunks1
                {
                    Chunk1 = (IntPtr)buffer,
                    Length1 = chunk.Span.Length,
                };
                callback(chunks);
            }
        }).Run();
    }

    public NetworkedComponentId GetNetworkedComponentId<T>()
        => _componentIds[typeof(T).FullName!].Id;

    public NetworkedComponentId GetNetworkedComponentId(string typeFullName)
        => _componentIds[typeFullName].Id;

    public void RunQuery(NetworkedComponentId c1, EmbedQueryDelegate1 callbackPtr)
    {
        var callbacks = _aotEcsCallbacks[c1];
        callbacks.Query1(callbackPtr);
    }

    public void RunQuery(NetworkedComponentId c1, NetworkedComponentId c2, EmbedQueryDelegate2 callback)
    {
        var types = _aotEcsCallbacks[c1].ComponentTypes;
        types.Add(_aotEcsCallbacks[c2].ComponentTypes);

        var query = world.Query(new QueryFilter().AllComponents(types));

        foreach (var archetype in query.Archetypes)
        {
            // archetype.Components returns a pointer to T[] and a count
            // since we cannot pin multiple arrays to managed types, we disable GC for the duration of the callback
            // this is probably extremely cursed
            var inNoGcRegion = GC.TryStartNoGCRegion(1 * 1024 * 1024, true); // 1 MB
            if (!inNoGcRegion)
            {
                throw new InvalidOperationException("Failed to start no GC region. Too many components in archetype?");
            }
            
            try
            {
                // get pointers to the chunks for each component
                var (ptr1, count1) = archetype.ComponentsAsUnsafeSpan(_aotEcsCallbacks[c1].ComponentType);
                var (ptr2, count2) = archetype.ComponentsAsUnsafeSpan(_aotEcsCallbacks[c2].ComponentType);

                // pack into Chunks2 struct and call the callback
                var chunks = new Chunks2
                {
                    Chunk1 = ptr1,
                    Length1 = count1,
                    Chunk2 = ptr2,
                    Length2 = count2
                };
                callback(chunks);
            }
            finally
            {
                GC.EndNoGCRegion();
            }
        }
    }

    public Type GetComponentType(NetworkedComponentId componentId)
        => _componentTypes[componentId];

    public DeliveryMethod GetNetworkedComponentDeliveryMethod<T>()
        => _componentIds[typeof(T).FullName!].DeliveryMethod;
}