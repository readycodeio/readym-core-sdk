using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
public partial class EcsApi
{
    private readonly QueryDelegate _query;
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
    private readonly GetComponentSlotDelegate _getComponentSlot;
    private readonly ComponentRegistry _registry;

    internal EcsApi(EcsApiPointers pointers, ComponentRegistry registry)
    {
        _registry = registry;
        _query = Marshal.GetDelegateForFunctionPointer<QueryDelegate>(pointers.Query);
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
        _getComponentSlot = Marshal.GetDelegateForFunctionPointer<GetComponentSlotDelegate>(pointers.GetComponentSlot);
    }

    /// <summary>
    /// Create a server-owned networked entity in no particular scope.
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateEntity(ArchetypeId archetypeId)
    {
        return new Entity(_createNetworkedEntity(archetypeId, 0, default), _getComponentSlot, _registry);
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
        return new Entity(_createNetworkedEntity(archetypeId, 1, owner), _getComponentSlot, _registry);
    }

    /// <summary>
    /// Creates a networked entity in the Global scope. The entity is owned by the server.
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateGlobalEntity(ArchetypeId archetypeId)
    {
        return new Entity(_createNetworkedEntity(archetypeId, 0, default), _getComponentSlot, _registry);
    }

    /// <summary>
    /// Creates a networked entity in the Global scope, with an owner override.
    /// </summary>
    /// <param name="archetypeId">The entity Archetype.</param>
    /// <param name="owner">The owner of the entity.</param>
    /// <returns>The created entity.</returns>
    public Entity CreateGlobalEntity(ArchetypeId archetypeId, PlayerId owner)
    {
        return new Entity(_createNetworkedEntity(archetypeId, 1, owner), _getComponentSlot, _registry);
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
        return new Entity(_createNetworkedPlayerEntity(archetypeId, playerId, 0, default), _getComponentSlot, _registry);
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
        return new Entity(_createNetworkedPlayerEntity(archetypeId, playerId, 1, owner), _getComponentSlot, _registry);
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
        return new Entity(_createNetworkedAreaEntity(archetypeId, areaId, 0, default), _getComponentSlot, _registry);
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
        return new Entity(_createNetworkedAreaEntity(archetypeId, areaId, 1, owner), _getComponentSlot, _registry);
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
        return new Entity(_createNetworkedCellEntity(archetypeId, cellId, 0, default), _getComponentSlot, _registry);
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
        return new Entity(_createNetworkedCellEntity(archetypeId, cellId, 1, owner), _getComponentSlot, _registry);
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
        return new Entity(_createLocalEntity(archetypeId), _getComponentSlot, _registry);
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

    public void SetComponent<T>(int entityId, in T component) where T : struct
        => GetComponentRef<T>(entityId) = component;

    /// <summary>False when the entity is gone or does not carry the component.</summary>
    public bool TryGetComponent<T>(int entityId, out T component) where T : struct
    {
        var slot = Locate<T>(entityId);

        if (!slot.Found())
        {
            component = default;
            return false;
        }

        component = SlotRef<T>(slot);
        return true;
    }

    public bool HasComponent<T>(int entityId) where T : struct => Locate<T>(entityId).Found();

    public ref T GetComponentRef<T>(int entityId) where T : struct => ref SlotRef<T>(Locate<T>(entityId));

    private unsafe ComponentSlot Locate<T>(int entityId) where T : struct
    {
        ComponentSlot slot;
        _getComponentSlot(entityId, _registry.ResolveComponentId<T>(), &slot);
        return slot;
    }

    [ThreadStatic]
    private static ChunkCallbackState _tlsState;

    private struct ChunkCallbackState
    {
        public object? Callback;
        public IntPtr State;
        public object? ManagedState;
    }

    /// <summary>
    /// Reference to element 0 of a chunk slot, whichever side owns the array.
    /// </summary>
    /// <remarks>
    /// A mod-owned slot carries its heap's handle rather than an address, because a mod component
    /// may hold managed references and so can never be pinned. Resolving it here yields a reference
    /// the GC tracks, so the array is free to move while the callback runs. An AOT-owned slot is in
    /// the pinned object heap, so its address is used as-is. Both come back as a <c>ref</c>, which
    /// is what lets the generated chunk loops be written once for either kind.
    /// </remarks>
    private static unsafe ref T ChunkBase<T>(in ChunkComponent comp)
        where T : struct
    {
        if (comp.HeapSelf != IntPtr.Zero)
        {
            var heap = (TypedComponentHeap<T>)GCHandle.FromIntPtr(comp.HeapSelf).Target!;
            return ref heap.GetRef(0);
        }

        return ref Unsafe.AsRef<T>((void*)comp.Data);
    }

    /// <summary>
    /// Advances a chunk slot reference by <paramref name="index"/> elements, using the stride the
    /// owning side reported rather than this side's view of the struct size.
    /// </summary>
    private static ref T ChunkElement<T>(ref T first, int index, int stride)
        where T : struct
        => ref Unsafe.As<byte, T>(ref Unsafe.Add(ref Unsafe.As<T, byte>(ref first), index * stride));

    /// <summary>
    /// Reference to a single component, whichever side owns it. Same contract as the chunk case: a
    /// mod-owned component is reached through a tracked ref into the mod's own array, an AOT-owned
    /// one through its pinned address.
    /// </summary>
    internal static unsafe ref T SlotRef<T>(scoped in ComponentSlot slot)
        where T : struct
    {
        if (slot.HeapSelf != IntPtr.Zero)
        {
            var heap = (TypedComponentHeap<T>)GCHandle.FromIntPtr(slot.HeapSelf).Target!;
            return ref heap.GetRef(slot.Index);
        }

        return ref Unsafe.AsRef<T>((void*)slot.Data);
    }

    internal Entity EntityFrom(int entityId)
    {
        return new Entity(entityId, _getComponentSlot, _registry);
    }
}
