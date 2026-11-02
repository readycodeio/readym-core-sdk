﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Ecs.Components;
using ReadyM.Relay.Server.Sdk.Interop;

namespace ReadyM.Relay.Server.Sdk.Ecs;

/// <summary>Reads and writes a component of an entity by id, whether the server or a mod owns its type.</summary>
internal sealed class EcsApi
{
    private readonly GetComponentSlotDelegate _getComponentSlot;
    private readonly ModComponentIds _componentIds;

    internal EcsApi(EcsApiPointers pointers, ModComponentIds componentIds)
    {
        _componentIds = componentIds;
        _getComponentSlot = Marshal.GetDelegateForFunctionPointer<GetComponentSlotDelegate>(pointers.GetComponentSlot);
    }

    public bool HasComponent<T>(int entityId) where T : struct => Locate<T>(entityId).Found();

    public ref T GetComponentRef<T>(int entityId) where T : struct => ref SlotRef<T>(Locate<T>(entityId));

    private unsafe ComponentSlot Locate<T>(int entityId) where T : struct
    {
        ComponentSlot slot;
        _getComponentSlot(RawEntities.FromId(entityId), 0, _componentIds.Resolve<T>(), &slot);
        return slot;
    }

    /// <summary>
    /// A mod-owned component is reached through a tracked ref into the mod's own array, which may move, an AOT-owned
    /// one through its pinned address.
    /// </summary>
    private static unsafe ref T SlotRef<T>(scoped in ComponentSlot slot)
        where T : struct
    {
        if (slot.HeapSelf != IntPtr.Zero)
        {
            var heap = (TypedComponentHeap<T>)GCHandle.FromIntPtr(slot.HeapSelf).Target!;
            return ref heap.GetRef(slot.Index);
        }

        return ref Unsafe.AsRef<T>((void*)slot.Data);
    }
}
