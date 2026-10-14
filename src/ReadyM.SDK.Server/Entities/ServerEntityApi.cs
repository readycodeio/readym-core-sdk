using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Mapping.Data;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Ecs;
using ReadyM.Relay.Server.Sdk.Ecs.Components;
using ReadyM.Relay.Server.Sdk.Interop;
using ReadyM.SDK.Archetypes;
using ReadyM.SDK.Chunks;
using ReadyM.SDK.Entities;
using ReadyM.SDK.Exceptions;
using Yooni.Native.Container;
using Yooni.Native.LowLevel;

namespace ReadyM.SDK.Server.Entities;

internal sealed class ServerEntityApi : IEntityApi, IChunkSource
{
    private readonly QueryDelegate _query;
    private readonly GetComponentSlotDelegate _getComponentSlot;
    private readonly SetComponentDelegate _setComponent;
    private readonly FindByIndexDelegate _findByIndex;
    private readonly QueryInScopeDelegate _queryInScope;
    private readonly TryGetEntityScopeDelegate _tryGetEntityScope;
    private readonly IsEntityAliveDelegate _isEntityAlive;
    private readonly CreateLocalEntityDelegate _createLocalEntity;
    private readonly CreateLocalEntityInScopeDelegate _createLocalEntityInScope;
    private readonly CreateNetworkedEntityDelegate _createNetworkedEntity;
    private readonly CreateNetworkedEntityInScopeDelegate _createNetworkedEntityInScope;
    private readonly DeleteNetworkedEntityDelegate _deleteEntity;
    private readonly RegisterArchetypeDelegate _registerArchetype;
    private readonly ResolveArchetypeDelegate _resolveArchetype;
    private readonly AddArchetypeTagDelegate? _addArchetypeTag;
    private readonly ConcurrentDictionary<ComponentSet, ArchetypeId> _archetypeIds = new();
    private readonly ModComponentIds _registry;
    private readonly ConcurrentDictionary<ComponentSet, int[]> _componentIds = new();
    private readonly ComponentIndexes _indexes = new();
    private QueryScope _scope = new();

    public ServerEntityApi(EcsApiPointers pointers, ArchetypePointers archetypes, ModComponentIds registry)
    {
        _registry = registry;
        _query = Marshal.GetDelegateForFunctionPointer<QueryDelegate>(pointers.Query);
        _getComponentSlot = Marshal.GetDelegateForFunctionPointer<GetComponentSlotDelegate>(pointers.GetComponentSlot);
        _setComponent = Marshal.GetDelegateForFunctionPointer<SetComponentDelegate>(pointers.SetComponent);
        _findByIndex = Marshal.GetDelegateForFunctionPointer<FindByIndexDelegate>(pointers.FindByIndex);
        _queryInScope = Marshal.GetDelegateForFunctionPointer<QueryInScopeDelegate>(pointers.QueryInScope);
        _tryGetEntityScope = Marshal.GetDelegateForFunctionPointer<TryGetEntityScopeDelegate>(pointers.TryGetEntityScope);
        _isEntityAlive = Marshal.GetDelegateForFunctionPointer<IsEntityAliveDelegate>(pointers.IsEntityAlive);
        _createLocalEntity = Marshal.GetDelegateForFunctionPointer<CreateLocalEntityDelegate>(pointers.CreateLocalEntity);
        _createLocalEntityInScope = Marshal.GetDelegateForFunctionPointer<CreateLocalEntityInScopeDelegate>(pointers.CreateLocalEntityInScope);
        _createNetworkedEntity = Marshal.GetDelegateForFunctionPointer<CreateNetworkedEntityDelegate>(pointers.CreateNetworkedEntity);
        _createNetworkedEntityInScope = Marshal.GetDelegateForFunctionPointer<CreateNetworkedEntityInScopeDelegate>(pointers.CreateNetworkedEntityInScope);
        _deleteEntity = Marshal.GetDelegateForFunctionPointer<DeleteNetworkedEntityDelegate>(pointers.DeleteNetworkedEntity);
        _registerArchetype = Marshal.GetDelegateForFunctionPointer<RegisterArchetypeDelegate>(archetypes.RegisterArchetype);
        _resolveArchetype = Marshal.GetDelegateForFunctionPointer<ResolveArchetypeDelegate>(archetypes.ResolveArchetype);
        _addArchetypeTag = archetypes.AddArchetypeTag == IntPtr.Zero
            ? null
            : Marshal.GetDelegateForFunctionPointer<AddArchetypeTagDelegate>(archetypes.AddArchetypeTag);
    }

    /// Whether the entity is networked is what its archetype declared, whatever components it holds.
    /// <param name="owner">Null leaves it the server's.</param>
    public RawEntity Create(ComponentSet components, RawEntity? scope = null, PlayerId? owner = null)
    {
        _scope.RefuseIfInQuery("Creating an entity");

        var archetype = ArchetypeOf(components);
        var named = owner.HasValue ? (byte)1 : (byte)0;

        var created = scope switch
        {
            { } holder when components.Replicated
                => _createNetworkedEntityInScope(archetype, holder, named, owner ?? default),
            { } holder => _createLocalEntityInScope(archetype, holder),
            _ when components.Replicated => _createNetworkedEntity(archetype, named, owner ?? default),
            _ => _createLocalEntity(archetype)
        };

        // Only a scoped create can come back empty, and that is the scope having gone.
        if (scope is { } asked && created.Id == 0)
            throw new InvalidEntityException($"Scope {asked.Id} is gone.");

        return Created(created, components);
    }

    private ArchetypeId ArchetypeOf(ComponentSet components)
        => _archetypeIds.GetOrAdd(components, static (set, self) => self.Tagged(set, self.Bound(set) ?? self.Register(set)), this);

    /// <summary>
    /// Settles the archetype of every shape that declared tags.
    /// </summary>
    internal int ApplyDeclaredTags()
    {
        var applied = 0;

        foreach (var shape in ArchetypeRegistry.TaggedShapes())
        {
            if (Activator.CreateInstance(shape) is not IArchetypeQueryable queryable)
                continue;

            var components = ArchetypeRegistry.SetFor(shape, queryable.Components);

            if (components.Replicated && !Binds(shape))
                continue;

            ArchetypeOf(components);
            applied++;
        }

        return applied;
    }

    /// <summary>Whether this game registers an archetype for the shape.</summary>
    private bool Binds(Type shape)
        => shape.FullName is { } name && _resolveArchetype(new NativeString256(name, useWide: false)) >= 0;

    /// <summary>Puts the shape's [Tag]s on the archetype, once, as it is first resolved.</summary>
    private ArchetypeId Tagged(ComponentSet components, ArchetypeId archetype)
    {
        if (_addArchetypeTag is null
            || ArchetypeRegistry.ShapeOf(components) is not { } shape)
            return archetype;

        foreach (var tag in ArchetypeRegistry.TagsFor(shape))
        {
            if (tag.FullName is not { } name)
                continue;

            if (_addArchetypeTag(archetype, new NativeString256(name, useWide: false)) == 0)
                throw new InvalidOperationException(
                    $"{shape.FullName} asks for the tag {name}, which this game's ECS does not know. A tag "
                    + "reaches the schema by being registered with the rest of them before the mods load.");
        }

        return archetype;
    }

    /// The archetype this game registered for the shape, or null when it registers none.
    private ArchetypeId? Bound(ComponentSet components)
    {
        if (ArchetypeRegistry.ShapeOf(components)?.FullName is not { } shape)
            return null;

        var id = _resolveArchetype(new NativeString256(shape, useWide: false));

        if (id >= 0)
            return new ArchetypeId((byte)id);

        if (components.Replicated)
            throw new InvalidOperationException(
                $"{shape} is declared replicated, but this game registers no archetype for it. Bind it "
                + "with IArchetypeShapeBindings on both the client and the server, or the entity would "
                + "reach the other side under an id only this one knows. An archetype that never leaves "
                + "this side is [Archetype(replicated: false)].");

        return null;
    }

    // A component holding a native collection has no memory until this runs, so it happens as the
    // entity is created rather than being left to whoever writes the shape first. What a shape asked
    // to run follows, once the entity is whole.
    private RawEntity Created(RawEntity rawEntity, ComponentSet components)
    {
        var handle = new EntityHandle(rawEntity, this);

        if (NativeInitRegistry.Any)
            NativeInitRegistry.InitAll(handle, components);

        if (CreateHandlerRegistry.Any)
            CreateHandlerRegistry.RunAll(handle);

        return rawEntity;
    }


    public bool Delete(RawEntity rawEntity)
    {
        if (!IsAlive(rawEntity))
            return false;

        if (_scope.InQuery)
            return _scope.Mark(rawEntity);

        return _deleteEntity(rawEntity, MatchRevision) != 0;
    }

    public void EnterQuery() => _scope.Enter();

    public void LeaveQuery()
    {
        if (!_scope.Leave())
            return;

        _scope.BeginDrain();

        foreach (var rawEntity in _scope.Pending)
            _deleteEntity(rawEntity, MatchRevision);

        _scope.Clear();
    }

    private ArchetypeId Register(ComponentSet components)
    {
        var ids = new List<int>(ResolveIds(components));

        foreach (var component in components.Types)
        {
            if (ChangeComponents.Of(component) is { } change && _registry.TryResolve(change, out var changeId) && !ids.Contains(changeId))
                ids.Add(changeId);
        }

        var native = new NativeList<int>(ids.Count, AllocatorKind.Default);

        foreach (var id in ids)
            native.Add(id);

        // Unnamed: an SDK shape declares no stable name yet, so its entities are not persisted.
        return _registerArchetype(NativeString256.Null, native);
    }

    public unsafe bool TryGetScope(RawEntity entity, out RawEntity scope)
    {
        RawEntity found;

        if (_tryGetEntityScope(entity, &found) == 0)
        {
            scope = default;
            return false;
        }

        scope = found;
        return true;
    }

    public unsafe EntityBuffer CollectInScope(RawEntity scope, ComponentSet components)
    {
        var ids = ResolveIds(components);
        var buffer = EntityBuffer.Rent();

        var previous = _collecting;
        _collecting = buffer;

        try
        {
            fixed (int* componentIds = ids)
            {
                _queryInScope(componentIds, ids.Length, scope, CollectEntities);
            }
        }
        finally
        {
            _collecting = previous;
        }

        return buffer;
    }

    private static readonly unsafe EntityListCallback CollectEntities = static (entities, count) =>
    {
        if (count > 0)
            new ReadOnlySpan<RawEntity>((void*)entities, count).CopyTo(_collecting!.Reserve(count));
    };

    public bool IsAlive(RawEntity rawEntity)
        => !_scope.IsPending(rawEntity) && _isEntityAlive(rawEntity, MatchRevision) != 0;

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

    public bool Allows(RawEntity rawEntity, Type component, WriteKind kind) => true;

    public bool ShouldApplyToGame(RawEntity rawEntity, Type component) => false;

    public bool Mirrors<TComponent, TValue>(
        RawEntity rawEntity,
        in TComponent component,
        Field<TComponent, TValue> field
    ) where TComponent : struct, IComponent
        => true;

    public bool Write<TComponent, TValue>(
        RawEntity rawEntity,
        ref TComponent component,
        Field<TComponent, TValue> field,
        TValue value,
        WriteKind kind
    ) where TComponent : struct, IComponent
    {
        field.SetFromApi(ref component, value, rawEntity.Id);
        return true;
    }

    public int ComponentIdOf(Type type) => _registry.Resolve(type);

    public unsafe ComponentRef Locate(RawEntity rawEntity, int componentId)
    {
        if (_scope.IsPending(rawEntity))
            throw new InvalidEntityException($"Entity {rawEntity.Id} was deleted by the running query.");

        ComponentSlot slot;
        _getComponentSlot(rawEntity, MatchRevision, componentId, &slot);

        if (slot.HeapSelf != IntPtr.Zero)
            return new ComponentRef(((IComponentArray)GCHandle.FromIntPtr(slot.HeapSelf).Target!).Components, slot.Index);

        return slot.Data != IntPtr.Zero ? new ComponentRef(slot.Data) : default;
    }

    /// Writes the whole component and moves it in the index kept beside it.
    public unsafe void ReplaceIndexed<TComponent, TKey>(RawEntity rawEntity, TComponent component)
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull
    {
        var id = _registry.Resolve<TComponent>();

        ComponentSlot slot;
        _getComponentSlot(rawEntity, MatchRevision, id, &slot);

        if (!slot.Found())
            throw Missing<TComponent>(rawEntity);

        if (slot.HeapSelf == IntPtr.Zero)
        {
            var copy = component;

            switch (_setComponent(rawEntity, MatchRevision, id, &copy, Unsafe.SizeOf<TComponent>()))
            {
                case IndexResult.NoIndex:
                    throw NoIndex<TComponent, TKey>();
                case IndexResult.Missed:
                    throw Missing<TComponent>(rawEntity);
            }

            return;
        }

        ref var stored = ref SlotRef<TComponent>(slot);

        _indexes.Forget<TComponent, TKey>(stored.GetIndexedValue(), rawEntity);

        stored = component;

        _indexes.Remember<TComponent, TKey>(component.GetIndexedValue(), rawEntity);
    }

    public unsafe bool TryFindByIndex<TComponent, TKey>(TKey key, out RawEntity entity)
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull
    {
        var id = _registry.Resolve<TComponent>();

        if (_indexes.TryFind<TComponent, TKey>(key, out entity))
        {
            if (Holds<TComponent, TKey>(entity, id, key))
                return true;

            _indexes.Forget<TComponent, TKey>(key, entity);
        }

        RawEntity found;
        var copy = key;

        switch (_findByIndex(id, &copy, Unsafe.SizeOf<TKey>(), &found))
        {
            case IndexResult.Done:
                entity = found;
                return true;
            case IndexResult.NoIndex:
                throw NoIndex<TComponent, TKey>();
            case IndexResult.ModOwned:
                return TryFindByWalking<TComponent, TKey>(id, key, out entity);
            default:
                entity = default;
                return false;
        }
    }

    private bool TryFindByWalking<TComponent, TKey>(int id, TKey key, out RawEntity entity)
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull
    {
        var carriers = CollectMatching(ComponentSet.Of<TComponent>());

        try
        {
            for (var i = 0; i < carriers.Count; i++)
            {
                if (!Holds<TComponent, TKey>(carriers[i], id, key))
                    continue;

                entity = carriers[i];

                // So the next lookup by the same key is answered without walking again.
                _indexes.Remember<TComponent, TKey>(key, entity);
                return true;
            }
        }
        finally
        {
            carriers.Return();
        }

        entity = default;
        return false;
    }

    private bool Holds<TComponent, TKey>(RawEntity entity, int id, TKey key)
        where TComponent : struct, IIndexedComponent<TKey> where TKey : notnull
    {
        if (!IsAlive(entity))
            return false;

        var located = Locate(entity, id);

        return located.Found
               && EqualityComparer<TKey>.Default.Equals(located.As<TComponent>().GetIndexedValue(), key);
    }

    private static InvalidOperationException NoIndex<TComponent, TKey>()
        => new($"This server keeps no index for {typeof(TComponent).Name}, so it cannot look one up by "
               + $"{typeof(TKey).Name}. The game registers it with RegisterIndexedComponent<{typeof(TComponent).Name}, "
               + $"{typeof(TKey).Name}>(); until then, find it with a query.");

    private static ref TComponent SlotRef<TComponent>(scoped in ComponentSlot slot) where TComponent : struct
    {
        var heap = (TypedComponentHeap<TComponent>)GCHandle.FromIntPtr(slot.HeapSelf).Target!;

        return ref heap.GetRef(slot.Index);
    }

    private static ComponentNotFoundException Missing<T>(RawEntity rawEntity)
        => new($"Entity {rawEntity.Id} does not carry {typeof(T).Name}.");

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

    EntityHandle IChunkSource.Prototype => new(default, this);

    unsafe ChunkBuffer IChunkSource.Collect(ComponentSet components)
    {
        var ids = ResolveIds(components);
        var buffer = ChunkBuffer.Rent(ids.Length);
        var previous = _collectingChunks;
        _collectingChunks = buffer;

        try
        {
            fixed (int* componentIds = ids)
            {
                _query(componentIds, ids.Length, CollectChunk);
            }
        }
        finally
        {
            _collectingChunks = previous;
        }

        return buffer;
    }

    [ThreadStatic]
    private static ChunkBuffer? _collectingChunks;

    private static readonly unsafe ChunkCallback CollectChunk = static (entities, comps, n, count) =>
    {
        var slots = _collectingChunks!.Append(entities, count);
        for (var i = 0; i < n; i++)
        {
            ref readonly var comp = ref comps[i];
            slots[i] = comp.HeapSelf != IntPtr.Zero
                ? new ChunkSlot(((IComponentArray)GCHandle.FromIntPtr(comp.HeapSelf).Target!).Components, comp.Stride)
                : new ChunkSlot(comp.Data, comp.Stride);
        }
    };


    private int[] ResolveIds(ComponentSet components) => _componentIds.GetOrAdd(components, static (set, self) =>
    {
        if (set.Types.Length == 0)
            throw new NotSupportedException(
                "An archetype with no components cannot be queried on the server: its identity there is its component set.");

        var ids = new int[set.Types.Length];

        for (var i = 0; i < ids.Length; i++)
            ids[i] = self._registry.Resolve(set.Types[i]);

        return ids;
    }, this);

    // A v1 handle always carries the revision it was created with, so every call opts into the
    // check. The relay reports an identity whose id now holds another entity as gone.
    private const byte MatchRevision = 1;

    [ThreadStatic]
    private static EntityBuffer? _collecting;

    private static readonly unsafe ChunkCallback Collect =
        static (entities, _, _, count) => new ReadOnlySpan<RawEntity>((void*)entities, count).CopyTo(_collecting!.Reserve(count));
}