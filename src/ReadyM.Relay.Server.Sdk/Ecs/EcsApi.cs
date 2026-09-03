using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using ReadyM.Api.Multiplayer.Interop;
using ReadyM.Relay.Server.Sdk.Ecs.Components;
using ReadyM.Relay.Server.Sdk.Interop;

namespace ReadyM.Relay.Server.Sdk.Ecs;

/// <summary>
/// Mod-side ECS API. Wraps the function pointers exposed by the AOT server.
/// All component types - whether defined in the server binary or in this mod - are
/// identified by <c>int</c> component IDs assigned at registration time.
/// </summary>
public class EcsApi
{
    private readonly Query1WithIdsDelegate _query1WithIds;
    private readonly Query2WithIdsDelegate _query2WithIds;
    private readonly Query1Delegate _query1;
    private readonly Query2Delegate _query2;
    private readonly Query3Delegate _query3;
    private readonly Query4Delegate _query4;
    private readonly Query5Delegate _query5;
    private readonly Query6Delegate _query6;
    private readonly CreateNetworkedEntityDelegate _createNetworkedEntity;
    private readonly CreateNetworkedPlayerEntityDelegate _createNetworkedPlayerEntity;
    private readonly CreateNetworkedAreaEntityDelegate _createNetworkedAreaEntity;
    private readonly CreateNetworkedCellEntityDelegate _createNetworkedCellEntity;
    private readonly CreateLocalEntityDelegate _createLocalEntity;
    private readonly DeleteNetworkedEntityDelegate _deleteNetworkedEntity;
    private readonly DeleteEntityTreeDelegate _deleteEntityTree;
    private readonly SetParentDelegate _setParent;
    private readonly GetParentDelegate _getParent;
    private readonly GetChildrenDelegate _getChildren;
    private readonly GetComponentPointerDelegate _getComponentPointer;
    private readonly ComponentRegistry _registry;

    internal EcsApi(EcsApiPointers pointers, ComponentRegistry registry)
    {
        _registry = registry;
        _query1WithIds = Marshal.GetDelegateForFunctionPointer<Query1WithIdsDelegate>(pointers.Query1WithIds);
        _query2WithIds = Marshal.GetDelegateForFunctionPointer<Query2WithIdsDelegate>(pointers.Query2WithIds);
        _query1 = Marshal.GetDelegateForFunctionPointer<Query1Delegate>(pointers.Query1);
        _query2 = Marshal.GetDelegateForFunctionPointer<Query2Delegate>(pointers.Query2);
        _query3 = Marshal.GetDelegateForFunctionPointer<Query3Delegate>(pointers.Query3);
        _query4 = Marshal.GetDelegateForFunctionPointer<Query4Delegate>(pointers.Query4);
        _query5 = Marshal.GetDelegateForFunctionPointer<Query5Delegate>(pointers.Query5);
        _query6 = Marshal.GetDelegateForFunctionPointer<Query6Delegate>(pointers.Query6);
        _createNetworkedEntity = Marshal.GetDelegateForFunctionPointer<CreateNetworkedEntityDelegate>(pointers.CreateNetworkedEntity);
        _createNetworkedPlayerEntity = Marshal.GetDelegateForFunctionPointer<CreateNetworkedPlayerEntityDelegate>(pointers.CreateNetworkedPlayerEntity);
        _createNetworkedAreaEntity = Marshal.GetDelegateForFunctionPointer<CreateNetworkedAreaEntityDelegate>(pointers.CreateNetworkedAreaEntity);
        _createNetworkedCellEntity = Marshal.GetDelegateForFunctionPointer<CreateNetworkedCellEntityDelegate>(pointers.CreateNetworkedCellEntity);
        _createLocalEntity = Marshal.GetDelegateForFunctionPointer<CreateLocalEntityDelegate>(pointers.CreateLocalEntity);
        _deleteNetworkedEntity = Marshal.GetDelegateForFunctionPointer<DeleteNetworkedEntityDelegate>(pointers.DeleteNetworkedEntity);
        _deleteEntityTree = Marshal.GetDelegateForFunctionPointer<DeleteEntityTreeDelegate>(pointers.DeleteEntityTree);
        _setParent = Marshal.GetDelegateForFunctionPointer<SetParentDelegate>(pointers.SetParent);
        _getParent = Marshal.GetDelegateForFunctionPointer<GetParentDelegate>(pointers.GetParent);
        _getChildren = Marshal.GetDelegateForFunctionPointer<GetChildrenDelegate>(pointers.GetChildren);
        _getComponentPointer = Marshal.GetDelegateForFunctionPointer<GetComponentPointerDelegate>(pointers.GetComponentPointer);
    }

    /// <summary>
    /// Create a server-owned networked entity in no particular scope.
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateEntity(ArchetypeId archetypeId)
    {
        return new Entity(_createNetworkedEntity(archetypeId, 0, default), _getComponentPointer, _registry);
    }

    /// <summary>
    /// Create a networked entity in no particular scope, with an owner override.
    /// The owner override is used to determine which client can modify it.
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <param name="owner">The owner of the entity.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateEntity(ArchetypeId archetypeId, PlayerId owner)
    {
        return new Entity(_createNetworkedEntity(archetypeId, 1, owner), _getComponentPointer, _registry);
    }

    /// <summary>
    /// Creates a networked entity in the Global scope. The entity is owned by the server.
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateGlobalEntity(ArchetypeId archetypeId)
    {
        return new Entity(_createNetworkedEntity(archetypeId, 0, default), _getComponentPointer, _registry);
    }

    /// <summary>
    /// Creates a networked entity in the Global scope, with an owner override.
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <param name="owner">The owner of the entity.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateGlobalEntity(ArchetypeId archetypeId, PlayerId owner)
    {
        return new Entity(_createNetworkedEntity(archetypeId, 1, owner), _getComponentPointer, _registry);
    }

    /// <summary>
    /// Creates a networked entity in the Player scope.
    /// When the associated player entity is destroyed, this entity is destroyed too.
    /// The entity is owned by the server (the player can only read it, not modify it).
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <param name="playerId">The player in whose scope the entity is created.</param>
    /// <returns>The created entity.</returns>
    public Entity CreatePlayerEntity(ArchetypeId archetypeId, PlayerId playerId)
    {
        return new Entity(_createNetworkedPlayerEntity(archetypeId, playerId, 0, default), _getComponentPointer, _registry);
    }

    /// <summary>
    /// Creates a networked entity in the Player scope, with an owner override.
    /// Usually, the player in whose scope the entity is created should be the owner.
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <param name="playerId">The player in whose scope the entity is created.</param>
    /// <param name="owner">The owner of the entity.</param>
    /// <returns>The created entity.</returns>
    public Entity CreatePlayerEntity(ArchetypeId archetypeId, PlayerId playerId, PlayerId owner)
    {
        return new Entity(_createNetworkedPlayerEntity(archetypeId, playerId, 1, owner), _getComponentPointer, _registry);
    }

    /// <summary>
    /// Creates a networked entity in the Area scope.
    /// When the area entity is destroyed, this entity is destroyed too.
    /// The entity is owned by the server.
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <param name="areaId">The Area in whose scope the entity is created.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateAreaEntity(ArchetypeId archetypeId, AreaId areaId)
    {
        return new Entity(_createNetworkedAreaEntity(archetypeId, areaId, 0, default), _getComponentPointer, _registry);
    }

    /// <summary>
    /// Creates a networked entity in the Area scope, with an owner override.
    /// When the area entity is destroyed, this entity is destroyed too.
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <param name="areaId">The Area in whose scope the entity is created.</param>
    /// <param name="owner">The owner of the entity.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateAreaEntity(ArchetypeId archetypeId, AreaId areaId, PlayerId owner)
    {
        return new Entity(_createNetworkedAreaEntity(archetypeId, areaId, 1, owner), _getComponentPointer, _registry);
    }

    /// <summary>
    /// Creates a networked entity in the Cell scope.
    /// When the cell entity is destroyed, this entity is destroyed too.
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <param name="cellId">The Cell in whose scope the entity is created.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateCellEntity(ArchetypeId archetypeId, FullCellId cellId)
    {
        return new Entity(_createNetworkedCellEntity(archetypeId, cellId, 0, default), _getComponentPointer, _registry);
    }

    /// <summary>
    /// Creates a networked entity in the Cell scope, with an owner override.
    /// When the cell entity is destroyed, this entity is destroyed too.
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <param name="cellId">The Cell in whose scope the entity is created.</param>
    /// <param name="owner">The owner of the entity.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateCellEntity(ArchetypeId archetypeId, FullCellId cellId, PlayerId owner)
    {
        return new Entity(_createNetworkedCellEntity(archetypeId, cellId, 1, owner), _getComponentPointer, _registry);
    }

    /// <summary>
    /// Creates a networked entity in the Cell scope.
    /// When the cell entity is destroyed, this entity is destroyed too.
    /// The entity is owned by the server.
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <param name="areaId">The Cell's area.</param>
    /// <param name="cellId">The Cell in whose scope the entity is created.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateCellEntity(ArchetypeId archetypeId, AreaId areaId, CellId cellId)
    {
        return CreateCellEntity(archetypeId, new FullCellId(areaId, cellId));
    }

    /// <summary>
    /// Creates a networked entity in the Cell scope, with an owner override.
    /// When the cell entity is destroyed, this entity is destroyed too.
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <param name="areaId">The Cell's area.</param>
    /// <param name="cellId">The Cell in whose scope the entity is created.</param>
    /// <param name="ownerOverride">The owner of the entity.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateCellEntity(ArchetypeId archetypeId, AreaId areaId, CellId cellId, PlayerId ownerOverride)
    {
        return CreateCellEntity(archetypeId, new FullCellId(areaId, cellId), ownerOverride);
    }

    /// <summary>
    /// Creates a server-only entity: never replicated, invisible to clients.
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateLocalEntity(ArchetypeId archetypeId)
    {
        return new Entity(_createLocalEntity(archetypeId), _getComponentPointer, _registry);
    }

    /// <inheritdoc cref="CreateLocalEntity(ArchetypeId)"/>
    /// <param name="parentId">The entity that owns the new one. Deleting it with
    /// <see cref="DeleteEntityTree"/> deletes the new one too.</param>
    public Entity CreateLocalEntity(ArchetypeId archetypeId, int parentId)
    {
        var entity = CreateLocalEntity(archetypeId);
        _setParent(entity.Id, parentId);
        return entity;
    }

    /// <summary>
    /// Deletes a networked entity.
    /// </summary>
    /// <param name="entity">The entity to delete.</param>
    /// <returns>Whether the entity was deleted (true) or already gone (false).</returns>
    public bool DeleteEntity(in Entity entity)
    {
        return DeleteEntity(entity.Id);
    }

    /// <summary>
    /// Deletes a networked entity.
    /// </summary>
    /// <param name="entityId">The ID of the entity to delete.</param>
    /// <returns>Whether the entity was deleted (true) or already gone (false).</returns>
    public bool DeleteEntity(int entityId)
    {
        return _deleteNetworkedEntity(entityId) != 0;
    }

    /// <summary>
    /// Deletes an entity together with everything below it. Deleting a parent on its own leaves its
    /// children behind without one, so this is the call to use for anything that owns other entities.
    /// </summary>
    /// <param name="entityId">The ID of the entity to delete along with its children.</param>
    /// <returns>How many entities were deleted.</returns>
    public int DeleteEntityTree(int entityId)
    {
        return _deleteEntityTree(entityId);
    }

    /// <summary>
    /// Makes the child belong to the parent, replacing whatever parent it had.
    /// </summary>
    /// <returns>The index the child took among the parent's children, or -1 if it already was one.</returns>
    public int SetParent(int childId, int parentId)
    {
        return _setParent(childId, parentId);
    }

    /// <summary>0 when the entity has no parent.</summary>
    /// <param name="childId">The ID of the child entity.</param>
    /// <returns>The ID of the parent entity, or 0 if there is no parent.</returns>
    public int GetParent(int childId)
    {
        return _getParent(childId);
    }

    /// <summary>
    /// The children of an entity. This one allocates, so call it outside a query callback, where a
    /// no-GC region is held over raw component pointers.
    /// </summary>
    public int[] GetChildren(int parentId)
    {
        Span<int> probe = stackalloc int[16];
        var count = FillChildren(parentId, probe);

        if (count == 0)
            return [];

        if (count <= probe.Length)
            return probe[..count].ToArray();

        var children = new int[count];
        FillChildren(parentId, children);
        return children;
    }

    private unsafe int FillChildren(int parentId, Span<int> into)
    {
        fixed (int* buffer = into)
            return _getChildren(parentId, (IntPtr)buffer, into.Length);
    }

    /// <exclude />
    public delegate void EmbedForEachEntity<T1>(ref T1 c1, int entityId) where T1 : struct;

    /// <exclude />
    public delegate void EmbedForEachEntity<T1, T2>(ref T1 c1, ref T2 c2, int entityId)
        where T1 : struct where T2 : struct;

    /// <summary>
    /// Iterates a component along with the entity id it belongs to.
    /// </summary>
    public void QueryWithEntity<T>(EmbedForEachEntity<T> callback) where T : struct
    {
        var id = _registry.ResolveComponentId<T>();
        _tlsState.Callback = callback;
        try
        {
            _query1WithIds(id, static (ids, d, count, s) => IterateChunkWithIds1<T>(ids, d, count, s));
        }
        finally
        {
            _tlsState.Callback = null;
        }
    }

    /// <inheritdoc cref="QueryWithEntity{T}"/>
    public void QueryWithEntity<T1, T2>(EmbedForEachEntity<T1, T2> callback)
        where T1 : struct where T2 : struct
    {
        var id1 = _registry.ResolveComponentId<T1>();
        var id2 = _registry.ResolveComponentId<T2>();
        _tlsState.Callback = callback;
        try
        {
            _query2WithIds(id1, id2,
                static (ids, d1, d2, count, s1, s2) => IterateChunkWithIds2<T1, T2>(ids, d1, d2, count, s1, s2));
        }
        finally
        {
            _tlsState.Callback = null;
        }
    }

    private static unsafe void IterateChunkWithIds1<T>(IntPtr ids, IntPtr d, int count, int s)
        where T : struct
    {
        var cb = (EmbedForEachEntity<T>)_tlsState.Callback!;
        var p = (byte*)d;
        var entityIds = (int*)ids;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T>(p + i * s), entityIds[i]);
    }

    private static unsafe void IterateChunkWithIds2<T1, T2>(IntPtr ids, IntPtr d1, IntPtr d2, int count,
        int s1, int s2) where T1 : struct where T2 : struct
    {
        var cb = (EmbedForEachEntity<T1, T2>)_tlsState.Callback!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var entityIds = (int*)ids;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), entityIds[i]);
    }

    /// <summary>Writes a component by entity id.</summary>
    public void SetComponent<T>(int entityId, in T component) where T : struct
        => GetComponentRef<T>(entityId) = component;

    /// <summary>False when the entity is gone or does not carry the component.</summary>
    public unsafe bool TryGetComponent<T>(int entityId, out T component) where T : struct
    {
        var compId = _registry.ResolveComponentId<T>();
        var ptr = _getComponentPointer(entityId, compId);

        if (ptr == IntPtr.Zero)
        {
            component = default;
            return false;
        }

        component = Unsafe.AsRef<T>((void*)ptr);
        return true;
    }

    public bool HasComponent<T>(int entityId) where T : struct
    {
        var compId = _registry.ResolveComponentId<T>();
        var ptr = _getComponentPointer(entityId, compId);

        return ptr != IntPtr.Zero;
    }

    public unsafe ref T GetComponentRef<T>(int entityId) where T : struct
    {
        var compId = _registry.ResolveComponentId<T>();
        var ptr = _getComponentPointer(entityId, compId);

        return ref Unsafe.AsRef<T>((void*)ptr);
    }

    [ThreadStatic]
    private static ChunkCallbackState _tlsState;

    private struct ChunkCallbackState
    {
        public object? Callback;
        public IntPtr State;
        public object? ManagedState;
    }

    #region Query 1

    /// <exclude />
    public delegate void EmbedForEach<T1>(ref T1 c1)
        where T1 : struct;

    /// <exclude />
    public delegate void EmbedForEachState<T, TState>(ref T component, ref TState state)
        where T : struct;

    /// <exclude />
    public delegate void EmbedForEachStateManaged<T, in TState>(ref T component, TState state)
        where T : struct
        where TState : class;

    private static unsafe void IterateChunk1<T>(IntPtr d, int count, int s)
        where T : struct
    {
        var cb = (EmbedForEach<T>)_tlsState.Callback!;
        var p = (byte*)d;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T>(p + i * s));
    }

    private static unsafe void IterateChunk1State<T, TState>(IntPtr d, int count, int s)
        where T : struct where TState : unmanaged
    {
        var cb = (EmbedForEachState<T, TState>)_tlsState.Callback!;
        var sp = (TState*)_tlsState.State;
        var p = (byte*)d;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T>(p + i * s), ref *sp);
    }

    private static unsafe void IterateChunk1ManagedState<T, TState>(IntPtr d, int count, int s)
        where T : struct where TState : class
    {
        var cb = (EmbedForEachState<T, TState>)_tlsState.Callback!;
        var ms = (TState)_tlsState.ManagedState!;
        var p = (byte*)d;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T>(p + i * s), ref ms);
    }

    public void Query<T>(EmbedForEach<T> callback) where T : struct
    {
        var id = _registry.ResolveComponentId<T>();

        _tlsState.Callback = callback;
        try
        {
            _query1(id, static (d, count, s) => IterateChunk1<T>(d, count, s));
        }
        finally
        {
            _tlsState.Callback = null;
        }
    }

    public void Query<T, TState>(TState state, EmbedForEachStateManaged<T, TState> callback)
        where T : struct where TState : class
    {
        var id = _registry.ResolveComponentId<T>();

        _tlsState.Callback = callback;
        _tlsState.ManagedState = state;
        try
        {
            _query1(id, static (d, count, s) => IterateChunk1ManagedState<T, TState>(d, count, s));
        }
        finally
        {
            _tlsState.Callback = null;
            _tlsState.ManagedState = null;
        }
    }

    public void Query<T, TState>(ref TState state, EmbedForEachState<T, TState> callback)
        where T : struct where TState : unmanaged
    {
        var id = _registry.ResolveComponentId<T>();
        unsafe
        {
            fixed (TState* sp = &state)
            {
                var statePtr = (IntPtr)sp;

                _tlsState.Callback = callback;
                _tlsState.State = statePtr;
                try
                {
                    _query1(id, static (d, count, s) => IterateChunk1State<T, TState>(d, count, s));
                }
                finally
                {
                    _tlsState.Callback = null;
                    _tlsState.State = IntPtr.Zero;
                }
            }
        }
    }

    #endregion

    #region Query 2

    /// <exclude />
    public delegate void EmbedForEach<T1, T2>(ref T1 c1, ref T2 c2)
        where T1 : struct where T2 : struct;

    /// <exclude />
    public delegate void EmbedForEachState<T1, T2, TState>(ref T1 c1, ref T2 c2, ref TState state)
        where T1 : struct where T2 : struct;

    /// <exclude />
    public delegate void EmbedForEachStateManaged<T1, T2, in TState>(ref T1 c1, ref T2 c2, TState state)
        where T1 : struct where T2 : struct where TState : class;

    private static unsafe void IterateChunk2<T1, T2>(IntPtr d1, IntPtr d2, int count, int s1, int s2)
        where T1 : struct where T2 : struct
    {
        var cb = (EmbedForEach<T1, T2>)_tlsState.Callback!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2));
    }

    private static unsafe void IterateChunk2State<T1, T2, TState>(IntPtr d1, IntPtr d2, int count, int s1, int s2)
        where T1 : struct where T2 : struct where TState : unmanaged
    {
        var cb = (EmbedForEachState<T1, T2, TState>)_tlsState.Callback!;
        var sp = (TState*)_tlsState.State;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref *sp);
    }

    private static unsafe void IterateChunk2ManagedState<T1, T2, TState>(IntPtr d1, IntPtr d2, int count, int s1, int s2)
        where T1 : struct where T2 : struct where TState : class
    {
        var cb = (EmbedForEachState<T1, T2, TState>)_tlsState.Callback!;
        var ms = (TState)_tlsState.ManagedState!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref ms);
    }

    public void Query<T1, T2>(EmbedForEach<T1, T2> callback)
        where T1 : struct where T2 : struct
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();

        _tlsState.Callback = callback;
        try
        {
            _query2(c1, c2, static (d1, d2, count, s1, s2) => IterateChunk2<T1, T2>(d1, d2, count, s1, s2));
        }
        finally
        {
            _tlsState.Callback = null;
        }
    }

    public void Query<T1, T2, TState>(TState state, EmbedForEachStateManaged<T1, T2, TState> callback)
        where T1 : struct where T2 : struct where TState : class
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();

        _tlsState.Callback = callback;
        _tlsState.ManagedState = state;
        try
        {
            _query2(c1, c2, static (d1, d2, count, s1, s2) => IterateChunk2ManagedState<T1, T2, TState>(d1, d2, count, s1, s2));
        }
        finally
        {
            _tlsState.Callback = null;
            _tlsState.ManagedState = null;
        }
    }

    public void Query<T1, T2, TState>(ref TState state, EmbedForEachState<T1, T2, TState> callback)
        where T1 : struct where T2 : struct where TState : unmanaged
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        unsafe
        {
            fixed (TState* sp = &state)
            {
                var statePtr = (IntPtr)sp;

                _tlsState.Callback = callback;
                _tlsState.State = statePtr;
                try
                {
                    _query2(c1, c2, static (d1, d2, count, s1, s2) => IterateChunk2State<T1, T2, TState>(d1, d2, count, s1, s2));
                }
                finally
                {
                    _tlsState.Callback = null;
                    _tlsState.State = IntPtr.Zero;
                }
            }
        }
    }

    #endregion

    #region Query 3

    /// <exclude />
    public delegate void EmbedForEach<T1, T2, T3>(ref T1 c1, ref T2 c2, ref T3 c3)
        where T1 : struct where T2 : struct where T3 : struct;

    /// <exclude />
    public delegate void EmbedForEachState<T1, T2, T3, TState>(ref T1 c1, ref T2 c2, ref T3 c3, ref TState state)
        where T1 : struct where T2 : struct where T3 : struct;

    /// <exclude />
    public delegate void EmbedForEachStateManaged<T1, T2, T3, in TState>(ref T1 c1, ref T2 c2, ref T3 c3, TState state)
        where T1 : struct where T2 : struct where T3 : struct where TState : class;

    private static unsafe void IterateChunk3<T1, T2, T3>(IntPtr d1, IntPtr d2, IntPtr d3, int count, int s1, int s2, int s3)
        where T1 : struct where T2 : struct where T3 : struct
    {
        var cb = (EmbedForEach<T1, T2, T3>)_tlsState.Callback!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3));
    }

    private static unsafe void IterateChunk3State<T1, T2, T3, TState>(IntPtr d1, IntPtr d2, IntPtr d3, int count, int s1, int s2, int s3)
        where T1 : struct where T2 : struct where T3 : struct where TState : unmanaged
    {
        var cb = (EmbedForEachState<T1, T2, T3, TState>)_tlsState.Callback!;
        var sp = (TState*)_tlsState.State;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref *sp);
    }

    private static unsafe void IterateChunk3ManagedState<T1, T2, T3, TState>(IntPtr d1, IntPtr d2, IntPtr d3, int count, int s1, int s2, int s3)
        where T1 : struct where T2 : struct where T3 : struct where TState : class
    {
        var cb = (EmbedForEachState<T1, T2, T3, TState>)_tlsState.Callback!;
        var ms = (TState)_tlsState.ManagedState!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref ms);
    }

    public void Query<T1, T2, T3>(EmbedForEach<T1, T2, T3> callback)
        where T1 : struct where T2 : struct where T3 : struct
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();

        _tlsState.Callback = callback;
        try
        {
            _query3(c1, c2, c3, static (d1, d2, d3, count, s1, s2, s3) => IterateChunk3<T1, T2, T3>(d1, d2, d3, count, s1, s2, s3));
        }
        finally
        {
            _tlsState.Callback = null;
        }
    }

    public void Query<T1, T2, T3, TState>(TState state, EmbedForEachStateManaged<T1, T2, T3, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where TState : class
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();

        _tlsState.Callback = callback;
        _tlsState.ManagedState = state;
        try
        {
            _query3(c1, c2, c3, static (d1, d2, d3, count, s1, s2, s3) => IterateChunk3ManagedState<T1, T2, T3, TState>(d1, d2, d3, count, s1, s2, s3));
        }
        finally
        {
            _tlsState.Callback = null;
            _tlsState.ManagedState = null;
        }
    }

    public void Query<T1, T2, T3, TState>(ref TState state, EmbedForEachState<T1, T2, T3, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where TState : unmanaged
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        unsafe
        {
            fixed (TState* sp = &state)
            {
                var statePtr = (IntPtr)sp;

                _tlsState.Callback = callback;
                _tlsState.State = statePtr;
                try
                {
                    _query3(c1, c2, c3, static (d1, d2, d3, count, s1, s2, s3) => IterateChunk3State<T1, T2, T3, TState>(d1, d2, d3, count, s1, s2, s3));
                }
                finally
                {
                    _tlsState.Callback = null;
                    _tlsState.State = IntPtr.Zero;
                }
            }
        }
    }

    #endregion

    #region Query 4

    /// <exclude />
    public delegate void EmbedForEach<T1, T2, T3, T4>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct;

    /// <exclude />
    public delegate void EmbedForEachState<T1, T2, T3, T4, TState>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref TState state)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct;

    /// <exclude />
    public delegate void EmbedForEachStateManaged<T1, T2, T3, T4, in TState>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, TState state)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where TState : class;

    private static unsafe void IterateChunk4<T1, T2, T3, T4>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, int count, int s1, int s2, int s3, int s4)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
    {
        var cb = (EmbedForEach<T1, T2, T3, T4>)_tlsState.Callback!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4));
    }

    private static unsafe void IterateChunk4State<T1, T2, T3, T4, TState>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, int count, int s1, int s2, int s3, int s4)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where TState : unmanaged
    {
        var cb = (EmbedForEachState<T1, T2, T3, T4, TState>)_tlsState.Callback!;
        var sp = (TState*)_tlsState.State;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref *sp);
    }

    private static unsafe void IterateChunk4ManagedState<T1, T2, T3, T4, TState>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, int count, int s1, int s2, int s3, int s4)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where TState : class
    {
        var cb = (EmbedForEachState<T1, T2, T3, T4, TState>)_tlsState.Callback!;
        var ms = (TState)_tlsState.ManagedState!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref ms);
    }

    public void Query<T1, T2, T3, T4>(EmbedForEach<T1, T2, T3, T4> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();

        _tlsState.Callback = callback;
        try
        {
            _query4(c1, c2, c3, c4, static (d1, d2, d3, d4, count, s1, s2, s3, s4) => IterateChunk4<T1, T2, T3, T4>(d1, d2, d3, d4, count, s1, s2, s3, s4));
        }
        finally
        {
            _tlsState.Callback = null;
        }
    }

    public void Query<T1, T2, T3, T4, TState>(TState state, EmbedForEachStateManaged<T1, T2, T3, T4, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where TState : class
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();

        _tlsState.Callback = callback;
        _tlsState.ManagedState = state;
        try
        {
            _query4(c1, c2, c3, c4, static (d1, d2, d3, d4, count, s1, s2, s3, s4) => IterateChunk4ManagedState<T1, T2, T3, T4, TState>(d1, d2, d3, d4, count, s1, s2, s3, s4));
        }
        finally
        {
            _tlsState.Callback = null;
            _tlsState.ManagedState = null;
        }
    }

    public void Query<T1, T2, T3, T4, TState>(ref TState state, EmbedForEachState<T1, T2, T3, T4, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where TState : unmanaged
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        unsafe
        {
            fixed (TState* sp = &state)
            {
                var statePtr = (IntPtr)sp;

                _tlsState.Callback = callback;
                _tlsState.State = statePtr;
                try
                {
                    _query4(c1, c2, c3, c4, static (d1, d2, d3, d4, count, s1, s2, s3, s4) => IterateChunk4State<T1, T2, T3, T4, TState>(d1, d2, d3, d4, count, s1, s2, s3, s4));
                }
                finally
                {
                    _tlsState.Callback = null;
                    _tlsState.State = IntPtr.Zero;
                }
            }
        }
    }

    #endregion

    #region Query 5

    /// <exclude />
    public delegate void EmbedForEach<T1, T2, T3, T4, T5>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct;

    /// <exclude />
    public delegate void EmbedForEachState<T1, T2, T3, T4, T5, TState>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref TState state)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct;

    /// <exclude />
    public delegate void EmbedForEachStateManaged<T1, T2, T3, T4, T5, in TState>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, TState state)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where TState : class;

    private static unsafe void IterateChunk5<T1, T2, T3, T4, T5>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, int count, int s1, int s2, int s3, int s4, int s5)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
    {
        var cb = (EmbedForEach<T1, T2, T3, T4, T5>)_tlsState.Callback!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        var p5 = (byte*)d5;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref Unsafe.AsRef<T5>(p5 + i * s5));
    }

    private static unsafe void IterateChunk5State<T1, T2, T3, T4, T5, TState>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, int count, int s1, int s2, int s3, int s4, int s5)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where TState : unmanaged
    {
        var cb = (EmbedForEachState<T1, T2, T3, T4, T5, TState>)_tlsState.Callback!;
        var sp = (TState*)_tlsState.State;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        var p5 = (byte*)d5;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref Unsafe.AsRef<T5>(p5 + i * s5), ref *sp);
    }

    private static unsafe void IterateChunk5ManagedState<T1, T2, T3, T4, T5, TState>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, int count, int s1, int s2, int s3, int s4, int s5)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where TState : class
    {
        var cb = (EmbedForEachState<T1, T2, T3, T4, T5, TState>)_tlsState.Callback!;
        var ms = (TState)_tlsState.ManagedState!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        var p5 = (byte*)d5;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref Unsafe.AsRef<T5>(p5 + i * s5), ref ms);
    }

    public void Query<T1, T2, T3, T4, T5>(EmbedForEach<T1, T2, T3, T4, T5> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        var c5 = _registry.ResolveComponentId<T5>();

        _tlsState.Callback = callback;
        try
        {
            _query5(c1, c2, c3, c4, c5, static (d1, d2, d3, d4, d5, count, s1, s2, s3, s4, s5) => IterateChunk5<T1, T2, T3, T4, T5>(d1, d2, d3, d4, d5, count, s1, s2, s3, s4, s5));
        }
        finally
        {
            _tlsState.Callback = null;
        }
    }

    public void Query<T1, T2, T3, T4, T5, TState>(TState state, EmbedForEachStateManaged<T1, T2, T3, T4, T5, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where TState : class
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        var c5 = _registry.ResolveComponentId<T5>();

        _tlsState.Callback = callback;
        _tlsState.ManagedState = state;
        try
        {
            _query5(c1, c2, c3, c4, c5, static (d1, d2, d3, d4, d5, count, s1, s2, s3, s4, s5) => IterateChunk5ManagedState<T1, T2, T3, T4, T5, TState>(d1, d2, d3, d4, d5, count, s1, s2, s3, s4, s5));
        }
        finally
        {
            _tlsState.Callback = null;
            _tlsState.ManagedState = null;
        }
    }

    public void Query<T1, T2, T3, T4, T5, TState>(ref TState state, EmbedForEachState<T1, T2, T3, T4, T5, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where TState : unmanaged
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        var c5 = _registry.ResolveComponentId<T5>();
        unsafe
        {
            fixed (TState* sp = &state)
            {
                var statePtr = (IntPtr)sp;

                _tlsState.Callback = callback;
                _tlsState.State = statePtr;
                try
                {
                    _query5(c1, c2, c3, c4, c5, static (d1, d2, d3, d4, d5, count, s1, s2, s3, s4, s5) => IterateChunk5State<T1, T2, T3, T4, T5, TState>(d1, d2, d3, d4, d5, count, s1, s2, s3, s4, s5));
                }
                finally
                {
                    _tlsState.Callback = null;
                    _tlsState.State = IntPtr.Zero;
                }
            }
        }
    }

    #endregion

    #region Query 6

    /// <exclude />
    public delegate void EmbedForEach<T1, T2, T3, T4, T5, T6>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct;

    /// <exclude />
    public delegate void EmbedForEachState<T1, T2, T3, T4, T5, T6, TState>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref TState state)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct;

    /// <exclude />
    public delegate void EmbedForEachStateManaged<T1, T2, T3, T4, T5, T6, in TState>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, TState state)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where TState : class;

    private static unsafe void IterateChunk6<T1, T2, T3, T4, T5, T6>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, IntPtr d6, int count, int s1, int s2, int s3, int s4, int s5, int s6)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
    {
        var cb = (EmbedForEach<T1, T2, T3, T4, T5, T6>)_tlsState.Callback!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        var p5 = (byte*)d5;
        var p6 = (byte*)d6;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref Unsafe.AsRef<T5>(p5 + i * s5), ref Unsafe.AsRef<T6>(p6 + i * s6));
    }

    private static unsafe void IterateChunk6State<T1, T2, T3, T4, T5, T6, TState>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, IntPtr d6, int count, int s1, int s2, int s3, int s4, int s5, int s6)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where TState : unmanaged
    {
        var cb = (EmbedForEachState<T1, T2, T3, T4, T5, T6, TState>)_tlsState.Callback!;
        var sp = (TState*)_tlsState.State;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        var p5 = (byte*)d5;
        var p6 = (byte*)d6;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref Unsafe.AsRef<T5>(p5 + i * s5), ref Unsafe.AsRef<T6>(p6 + i * s6), ref *sp);
    }

    private static unsafe void IterateChunk6ManagedState<T1, T2, T3, T4, T5, T6, TState>(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, IntPtr d6, int count, int s1, int s2, int s3, int s4, int s5, int s6)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where TState : class
    {
        var cb = (EmbedForEachState<T1, T2, T3, T4, T5, T6, TState>)_tlsState.Callback!;
        var ms = (TState)_tlsState.ManagedState!;
        var p1 = (byte*)d1;
        var p2 = (byte*)d2;
        var p3 = (byte*)d3;
        var p4 = (byte*)d4;
        var p5 = (byte*)d5;
        var p6 = (byte*)d6;
        for (var i = 0; i < count; i++)
            cb(ref Unsafe.AsRef<T1>(p1 + i * s1), ref Unsafe.AsRef<T2>(p2 + i * s2), ref Unsafe.AsRef<T3>(p3 + i * s3), ref Unsafe.AsRef<T4>(p4 + i * s4), ref Unsafe.AsRef<T5>(p5 + i * s5), ref Unsafe.AsRef<T6>(p6 + i * s6), ref ms);
    }

    public void Query<T1, T2, T3, T4, T5, T6>(EmbedForEach<T1, T2, T3, T4, T5, T6> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        var c5 = _registry.ResolveComponentId<T5>();
        var c6 = _registry.ResolveComponentId<T6>();

        _tlsState.Callback = callback;
        try
        {
            _query6(c1, c2, c3, c4, c5, c6, static (d1, d2, d3, d4, d5, d6, count, s1, s2, s3, s4, s5, s6) => IterateChunk6<T1, T2, T3, T4, T5, T6>(d1, d2, d3, d4, d5, d6, count, s1, s2, s3, s4, s5, s6));
        }
        finally
        {
            _tlsState.Callback = null;
        }
    }

    public void Query<T1, T2, T3, T4, T5, T6, TState>(TState state, EmbedForEachStateManaged<T1, T2, T3, T4, T5, T6, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where TState : class
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        var c5 = _registry.ResolveComponentId<T5>();
        var c6 = _registry.ResolveComponentId<T6>();

        _tlsState.Callback = callback;
        _tlsState.ManagedState = state;
        try
        {
            _query6(c1, c2, c3, c4, c5, c6, static (d1, d2, d3, d4, d5, d6, count, s1, s2, s3, s4, s5, s6) => IterateChunk6ManagedState<T1, T2, T3, T4, T5, T6, TState>(d1, d2, d3, d4, d5, d6, count, s1, s2, s3, s4, s5, s6));
        }
        finally
        {
            _tlsState.Callback = null;
            _tlsState.ManagedState = null;
        }
    }

    public void Query<T1, T2, T3, T4, T5, T6, TState>(ref TState state, EmbedForEachState<T1, T2, T3, T4, T5, T6, TState> callback)
        where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct where TState : unmanaged
    {
        var c1 = _registry.ResolveComponentId<T1>();
        var c2 = _registry.ResolveComponentId<T2>();
        var c3 = _registry.ResolveComponentId<T3>();
        var c4 = _registry.ResolveComponentId<T4>();
        var c5 = _registry.ResolveComponentId<T5>();
        var c6 = _registry.ResolveComponentId<T6>();
        unsafe
        {
            fixed (TState* sp = &state)
            {
                var statePtr = (IntPtr)sp;

                _tlsState.Callback = callback;
                _tlsState.State = statePtr;
                try
                {
                    _query6(c1, c2, c3, c4, c5, c6, static (d1, d2, d3, d4, d5, d6, count, s1, s2, s3, s4, s5, s6) => IterateChunk6State<T1, T2, T3, T4, T5, T6, TState>(d1, d2, d3, d4, d5, d6, count, s1, s2, s3, s4, s5, s6));
                }
                finally
                {
                    _tlsState.Callback = null;
                    _tlsState.State = IntPtr.Zero;
                }
            }
        }
    }

    #endregion

    internal Entity EntityFrom(int entityId)
    {
        return new Entity(entityId, _getComponentPointer, _registry);
    }
}
