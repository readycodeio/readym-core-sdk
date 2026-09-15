using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Components;
using ReadyM.Relay.Server.Sdk.Interop;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Entity;
using IComponent = Friflo.Engine.ECS.IComponent;

namespace ReadyM.SDK.Server.Entity;

internal sealed class ServerEntityApi : IEntityApi
{
    private readonly QueryDelegate _query;
    private readonly GetComponentSlotDelegate _getComponentSlot;
    private readonly IsEntityAliveDelegate _isEntityAlive;
    private readonly ComponentRegistry _registry;
    private readonly ConcurrentDictionary<ComponentSet, int[]> _componentIds = new();

    internal ServerEntityApi(EcsApiPointers pointers, ComponentRegistry registry)
    {
        _registry = registry;
        _query = Marshal.GetDelegateForFunctionPointer<QueryDelegate>(pointers.Query);
        _getComponentSlot = Marshal.GetDelegateForFunctionPointer<GetComponentSlotDelegate>(pointers.GetComponentSlot);
        _isEntityAlive = Marshal.GetDelegateForFunctionPointer<IsEntityAliveDelegate>(pointers.IsEntityAlive);
    }

    public bool IsAlive(RawEntity rawEntity) => _isEntityAlive(rawEntity, MatchRevision) != 0;

    public unsafe bool HasComponents(RawEntity rawEntity, ComponentSet components)
    {
        if (!IsAlive(rawEntity))
            throw new InvalidEntityException($"Entity {rawEntity.Id} is gone.");

        if (components.Types.Length == 0)
            return true;

        foreach (var componentId in ResolveIds(components))
        {
            ComponentSlot slot;
            _getComponentSlot(rawEntity, MatchRevision, componentId, &slot);

            if (!slot.Found())
                return false;
        }

        return true;
    }

    public bool HasComponent<T>(RawEntity rawEntity) where T : struct, IComponent
        => Locate<T>(rawEntity).Found();

    public ref T GetComponent<T>(RawEntity rawEntity) where T : struct, IComponent
    {
        var slot = Locate<T>(rawEntity);

        if (!slot.Found())
            throw Missing<T>(rawEntity);

        return ref SlotRef<T>(slot);
    }

    public bool TryGetComponent<T>(RawEntity rawEntity, out T component) where T : struct, IComponent
    {
        var slot = Locate<T>(rawEntity);

        if (!slot.Found())
        {
            component = default;
            return false;
        }

        component = SlotRef<T>(slot);
        return true;
    }

    // TODO
    public void AddComponent<T>(RawEntity rawEntity) where T : struct, IComponent
        => throw new NotSupportedException($"Cannot add {typeof(T).Name} to entity {rawEntity.Id}: the server fixes an entity's components at creation.");

    // TODO
    public bool HasTag<T>(RawEntity rawEntity) where T : struct, ITag
        => throw new NotSupportedException($"Tag {typeof(T).Name} has no server representation.");

    // TODO
    public void SetTag<T>(RawEntity rawEntity, bool set) where T : struct, ITag
        => throw new NotSupportedException($"Tag {typeof(T).Name} has no server representation.");

    internal unsafe EntityBuffer CollectMatching(ComponentSet components)
    {
        var ids = ResolveIds(components);
        var buffer = EntityBuffer.Rent();

        var previous = _collecting;
        _collecting = buffer;

        try
        {
            fixed (int* componentIds = ids)
            {
                _query(componentIds, ids.Length, Collect);
            }
        }
        finally
        {
            _collecting = previous;
        }

        return buffer;
    }

    private int[] ResolveIds(ComponentSet components) => _componentIds.GetOrAdd(components, static (set, self) =>
    {
        if (set.Types.Length == 0)
            throw new NotSupportedException(
                "An archetype with no components cannot be queried on the server: its identity there is its component set.");

        var ids = new int[set.Types.Length];
        for (var i = 0; i < ids.Length; i++)
            ids[i] = self._registry.ResolveComponentId(set.Types[i]);

        return ids;
    }, this);

    // A v1 handle always carries the revision it was created with, so every call opts into the
    // check. The relay reports an identity whose id now holds another entity as gone.
    private const byte MatchRevision = 1;

    [ThreadStatic]
    private static EntityBuffer? _collecting;

    private static readonly unsafe ChunkCallback Collect =
        static (entities, _, _, count) => new ReadOnlySpan<RawEntity>((void*)entities, count).CopyTo(_collecting!.Reserve(count));

    private unsafe ComponentSlot Locate<T>(RawEntity rawEntity) where T : struct
    {
        ComponentSlot slot;
        _getComponentSlot(rawEntity, MatchRevision, _registry.ResolveComponentId<T>(), &slot);
        return slot;
    }

    private static unsafe ref T SlotRef<T>(scoped in ComponentSlot slot) where T : struct
    {
        if (slot.HeapSelf != IntPtr.Zero)
        {
            var heap = (TypedComponentHeap<T>)GCHandle.FromIntPtr(slot.HeapSelf).Target!;
            return ref heap.GetRef(slot.Index);
        }

        return ref Unsafe.AsRef<T>((void*)slot.Data);
    }

    private static ComponentNotFoundException Missing<T>(RawEntity rawEntity)
        => new($"Entity {rawEntity.Id} is gone or does not carry {typeof(T).Name}.");
}