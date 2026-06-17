using System.Runtime.CompilerServices;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Ecs.Components;

namespace ReadyM.Relay.Server.Sdk.Ecs;

public readonly struct Entity
{
    private readonly int _id;
    private readonly GetComponentPointerDelegate _getComponentPointer;
    private readonly ComponentRegistry _registry;

    internal Entity(int id, GetComponentPointerDelegate getComponentPointer, ComponentRegistry registry)
    {
        _id = id;
        _getComponentPointer = getComponentPointer;
        _registry = registry;
    }

    public unsafe ref T GetComponent<T>() where T : struct
    {
        var compId = _registry.ResolveComponentId<T>();
        var ptr = _getComponentPointer(_id, compId);
        return ref Unsafe.AsRef<T>((void*)ptr);
    }
}