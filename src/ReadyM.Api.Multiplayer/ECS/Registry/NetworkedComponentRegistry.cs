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
        public HostQueryDelegate Query;
    }

    private byte _nextComponentId;
    private readonly Dictionary<Type, (NetworkedComponentId Id, DeliveryMethod DeliveryMethod)> _componentIds = new();
    private readonly Dictionary<NetworkedComponentId, Type> _componentTypes = new();
    private readonly Dictionary<NetworkedComponentId, AotEcsCallbacks> _aotEcsCallbacks = new();

    public new INetworkedComponentRegistry RegisterComponent<T>(T defaultValue = default)
        where T : struct, INetworkedComponent
        => RegisterComponent(DeliveryMethod.Unreliable, defaultValue);

    public INetworkedComponentRegistry RegisterComponent<T>(DeliveryMethod deliveryMethod = DeliveryMethod.Unreliable, T defaultValue = default)
        where T : struct, INetworkedComponent
    {
        var id = new NetworkedComponentId(_nextComponentId++);
        _componentIds.Add(typeof(T), (id, deliveryMethod));
        _componentTypes.Add(id, typeof(T));

        _aotEcsCallbacks.Add(id, new AotEcsCallbacks
        {
            Query = RunQuery<T>
        });
        return base.RegisterComponent(defaultValue);
    }

    public NetworkedComponentId GetNetworkedComponentId(Type type)
        => _componentIds[type].Id;

    private unsafe void RunQuery<T>(EmbedQueryDelegate callback) where T : struct, IComponent
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
        => _componentIds[typeof(T)].Id;

    public void RunQuery(NetworkedComponentId componentId, EmbedQueryDelegate callbackPtr)
    {
        var callbacks = _aotEcsCallbacks[componentId];
        callbacks.Query(callbackPtr);
    }

    public Type GetComponentType(NetworkedComponentId componentId)
        => _componentTypes[componentId];

    public DeliveryMethod GetNetworkedComponentDeliveryMethod<T>()
        => _componentIds[typeof(T)].DeliveryMethod;
}