using System;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using Yooni.Native.Container;

namespace ReadyM.Api.Multiplayer.Interop;

internal delegate int GetComponentIdByNameDelegate(NativeString256 typeName);

internal delegate ArchetypeId RegisterArchetypeDelegate(NativeList<int> componentsSerialized);

internal delegate void ModifyArchetypeDelegate(ArchetypeId archetype, NativeList<int> componentsSerialized);

/// Adds components to an archetype the server owns.
internal delegate void AddArchetypeExtensionsDelegate(int archetype, NativeList<int> componentsSerialized);

// Entity identity crosses the boundary as a RawEntity, id plus revision, because the relay world
// recycles ids: the id freed by a delete is the next one handed out. A caller that kept an identity
// across ticks and passes matchRevision = 1 is told the entity is gone; one that passes 0 addresses
// whatever holds the id now, which is what the v0 API can offer and all it ever offered.
internal delegate RawEntity CreateNetworkedEntityDelegate(ArchetypeId archetype, byte hasOwnerOverride, PlayerId ownerOverride);

internal delegate RawEntity CreateNetworkedPlayerEntityDelegate(ArchetypeId archetype, PlayerId playerId, byte hasOwnerOverride, PlayerId ownerOverride);

internal delegate RawEntity CreateNetworkedAreaEntityDelegate(ArchetypeId archetype, AreaId areaId, byte hasOwnerOverride, PlayerId ownerOverride);

internal delegate RawEntity CreateNetworkedCellEntityDelegate(ArchetypeId archetype, FullCellId cellId, byte hasOwnerOverride, PlayerId ownerOverride);

/// Creates a server-only entity: no metadata, never replicated to clients.
internal delegate RawEntity CreateLocalEntityDelegate(ArchetypeId archetype);

/// <summary>
/// The same, held by a scope: the entity is removed when the scope is.
/// </summary>
/// <remarks>Returns a default identity when the scope is gone.</remarks>
internal delegate RawEntity CreateLocalEntityInScopeDelegate(ArchetypeId archetype, RawEntity scope);

/// 1 if the entity existed and was deleted, else 0.
internal delegate int DeleteNetworkedEntityDelegate(RawEntity entity, byte matchRevision);

/// Deletes an entity together with every entity below it in the tree.
internal delegate int DeleteEntityTreeDelegate(RawEntity entity, byte matchRevision);

// The tree calls below still take bare ids. They are v0 only, and relationships on the v1 surface
// are still open in the spec, so they get revisions when that lands.

/// Makes the child belong to the parent. Returns the index it took among the parent's children,
/// or -1 if it already was one of them.
internal delegate int SetParentDelegate(int childId, int parentId);

/// 0 when the entity has no parent.
internal delegate int GetParentDelegate(int childId);

/// Writes the parent's child ids into the buffer and returns how many children it has, which can
/// exceed the capacity. Nothing is written when the buffer is too small.
internal delegate int GetChildrenDelegate(int parentId, IntPtr buffer, int capacity);

/// 1 when the entity is in the world, else 0. The mod host has no store of its own, so
/// validity of a handle it kept across ticks can only be answered here.
internal delegate byte IsEntityAliveDelegate(RawEntity entity, byte matchRevision);

/// Locates one component of one entity, the same way a chunk slot is located: by address when the
/// AOT side owns it, by heap handle and index when a mod does.
internal unsafe delegate void GetComponentSlotDelegate(RawEntity entity, byte matchRevision, int componentType, ComponentSlot* slot);

// The five serialization callbacks below address a mod component by its heap and index rather than
// by address. The component lives in the embedded runtime, where the GC is free to relocate the
// array, so only the owning side can safely turn a slot into a reference. A zero heapSelf means
// "use your own scratch instance", which is how the AOT side drains bytes for an entity it does
// not have.
internal unsafe delegate int WriteSnapshotDelegate(IntPtr heapSelf, int index, byte* buffer, int bufferSize);

internal unsafe delegate int WriteDeltaDelegate(IntPtr heapSelf, int index, byte* buffer, int bufferSize);

internal unsafe delegate int ReadSnapshotDelegate(IntPtr heapSelf, int index, byte* buffer, int size);

internal unsafe delegate int ReadDeltaDelegate(IntPtr heapSelf, int index, byte* buffer, int size, byte clearDirty);

/// 1 if the component was changed from the API (a server override), else 0.
internal delegate byte ChangedFromApiDelegate(IntPtr heapSelf, int index);

/// Single component slot of an archetype chunk.
[StructLayout(LayoutKind.Sequential)]
internal struct ChunkComponent
{
    /// Address of element 0, set when the AOT side owns the array. Those live in the pinned object
    /// heap, so the address stays valid for as long as the mod side holds it.
    public IntPtr Data;


    /// GCHandle of the mod's own <c>TypedComponentHeap</c>, set when the mod owns the array. Zero
    /// otherwise. A mod component may hold managed references and therefore cannot be pinned, so no
    /// address for it is allowed to leave its runtime. The mod side resolves this handle and walks
    /// the array through a tracked ref, which the GC updates if it relocates the array.
    public IntPtr HeapSelf;

    /// Bytes per element as the owning side sees it.
    public int Stride;
}

/// One component of one entity, located the same way a <see cref="ChunkComponent"/> is. Both fields
/// zero means the entity is gone or does not carry the component.
[StructLayout(LayoutKind.Sequential)]
internal struct ComponentSlot
{
    /// Address of the component, set when the AOT side owns it.
    public IntPtr Data;

    /// GCHandle of the mod's own <c>TypedComponentHeap</c>, set when a mod owns it.
    public IntPtr HeapSelf;

    /// Index within that heap. Meaningful only alongside <see cref="HeapSelf"/>.
    public int Index;

    public readonly bool Found() => Data != IntPtr.Zero || HeapSelf != IntPtr.Zero;
}

/// Chunk callback for a query of any arity. <paramref name="comps"/> holds <paramref name="n"/>
/// slots in the order the components were requested, and <paramref name="entities"/> points at
/// <paramref name="count"/> <see cref="RawEntity"/> values.
internal unsafe delegate void ChunkCallback(IntPtr entities, ChunkComponent* comps, int n, int count);

/// Runs a query over <paramref name="n"/> component ids, one chunk callback per archetype.
internal unsafe delegate void QueryDelegate(int* componentIds, int n, ChunkCallback cb);

/// Writes a whole component, which is required for updating indices.
internal unsafe delegate byte SetComponentDelegate(RawEntity entity, byte matchRevision, int componentType, void* data, int size);

/// The entity whose indexed component holds this value. Returns 0 when none does.
/// <remarks>The value arrives as its own bytes, so only an unmanaged key can be asked for.</remarks>
internal unsafe delegate byte FindByIndexDelegate(int componentType, void* value, int size, RawEntity* found);

/// <summary>Receives the entities a scoped query matched, all at once.</summary>
internal unsafe delegate void EntityListCallback(IntPtr entities, int count);

/// <summary>
/// The entities a scope holds that carry every requested component.
/// </summary>
/// <remarks>
/// Answered from the scope's own links rather than by scanning the world, so it costs what the
/// scope holds. The result is a list of entities rather than chunks: the matches are scattered
/// through each archetype, so there are no contiguous runs to hand back.
/// </remarks>
internal unsafe delegate void QueryInScopeDelegate(int* componentIds, int n, RawEntity scope, EntityListCallback cb);

internal delegate int RegisterModComponentDelegate(ModComponentRegistration registration, NativeString256 displayName);