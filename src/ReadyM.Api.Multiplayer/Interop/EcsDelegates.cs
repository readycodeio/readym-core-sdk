// -------------------------------------------------------------------------
// Delegates and pointers - simplified now that IDs are plain ints
// -------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using Yooni.Native.Container;

namespace ReadyM.Api.Multiplayer.Interop;

internal delegate int GetComponentIdByNameDelegate(NativeString256 typeName);

internal delegate ArchetypeId RegisterArchetypeDelegate(NativeList<int> componentsSerialized);

internal delegate void ModifyArchetypeDelegate(ArchetypeId archetype, NativeList<int> componentsSerialized);

// Entity identity crosses the boundary as a RawEntity, id plus revision, because the relay world
// recycles ids: the id freed by a delete is the next one handed out. A caller that kept an identity
// across ticks and passes matchRevision = 1 is told the entity is gone; one that passes 0 addresses
// whatever holds the id now, which is what the v0 API can offer and all it ever offered.
internal delegate RawEntity CreateNetworkedEntityDelegate(ArchetypeId archetype, byte hasOwnerOverride, PlayerId ownerOverride);

internal delegate RawEntity CreateNetworkedPlayerEntityDelegate(ArchetypeId archetype, PlayerId playerId, byte hasOwnerOverride, PlayerId ownerOverride);

internal delegate RawEntity CreateNetworkedAreaEntityDelegate(ArchetypeId archetype, AreaId areaId, byte hasOwnerOverride, PlayerId ownerOverride);

internal delegate RawEntity CreateNetworkedCellEntityDelegate(ArchetypeId archetype, FullCellId cellId, byte hasOwnerOverride, PlayerId ownerOverride);

/// <summary>Creates a server-only entity: no metadata, never replicated to clients.</summary>
internal delegate RawEntity CreateLocalEntityDelegate(ArchetypeId archetype);

/// <summary>1 if the entity existed and was deleted, else 0.</summary>
internal delegate int DeleteNetworkedEntityDelegate(RawEntity entity, byte matchRevision);

/// <summary>Deletes an entity together with every entity below it in the tree.</summary>
internal delegate int DeleteEntityTreeDelegate(RawEntity entity, byte matchRevision);

// The tree calls below still take bare ids. They are v0 only, and relationships on the v1 surface
// are still open in the spec, so they get revisions when that lands.

/// <summary>
/// Makes the child belong to the parent. Returns the index it took among the parent's children,
/// or -1 if it already was one of them.
/// </summary>
internal delegate int SetParentDelegate(int childId, int parentId);

/// <summary>0 when the entity has no parent.</summary>
internal delegate int GetParentDelegate(int childId);

/// <summary>
/// Writes the parent's child ids into the buffer and returns how many children it has, which can
/// exceed the capacity. Nothing is written when the buffer is too small.
/// </summary>
internal delegate int GetChildrenDelegate(int parentId, IntPtr buffer, int capacity);

/// <summary>1 when the entity is in the world, else 0. The mod host has no store of its own, so
/// validity of a handle it kept across ticks can only be answered here.</summary>
internal delegate byte IsEntityAliveDelegate(RawEntity entity, byte matchRevision);

/// <summary>
/// Locates one component of one entity, the same way a chunk slot is located: by address when the
/// AOT side owns it, by heap handle and index when a mod does.
/// </summary>
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

/// <summary>1 if the component was changed from the API (a server override), else 0.</summary>
internal delegate byte ChangedFromApiDelegate(IntPtr heapSelf, int index);

/// <summary>
/// Single component slot of an archetype chunk.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct ChunkComponent
{
    /// <summary>
    /// Address of element 0, set when the AOT side owns the array. Those live in the pinned object
    /// heap, so the address stays valid for as long as the mod side holds it.
    /// </summary>
    public IntPtr Data;

    /// <summary>
    /// GCHandle of the mod's own <c>TypedComponentHeap</c>, set when the mod owns the array. Zero
    /// otherwise. A mod component may hold managed references and therefore cannot be pinned, so no
    /// address for it is allowed to leave its runtime. The mod side resolves this handle and walks
    /// the array through a tracked ref, which the GC updates if it relocates the array.
    /// </summary>
    public IntPtr HeapSelf;

    /// <summary>Bytes per element as the owning side sees it.</summary>
    public int Stride;
}

/// <summary>
/// One component of one entity, located the same way a <see cref="ChunkComponent"/> is. Both fields
/// zero means the entity is gone or does not carry the component.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct ComponentSlot
{
    /// <summary>Address of the component, set when the AOT side owns it.</summary>
    public IntPtr Data;

    /// <summary>GCHandle of the mod's own <c>TypedComponentHeap</c>, set when a mod owns it.</summary>
    public IntPtr HeapSelf;

    /// <summary>Index within that heap. Meaningful only alongside <see cref="HeapSelf"/>.</summary>
    public int Index;

    public readonly bool Found() => Data != IntPtr.Zero || HeapSelf != IntPtr.Zero;
}

/// <summary>
/// Chunk callback for a query of any arity. <paramref name="comps"/> holds <paramref name="n"/>
/// slots in the order the components were requested, and <paramref name="entities"/> points at
/// <paramref name="count"/> <see cref="RawEntity"/> values.
/// </summary>
internal unsafe delegate void ChunkCallback(IntPtr entities, ChunkComponent* comps, int n, int count);

/// <summary>Runs a query over <paramref name="n"/> component ids, one chunk callback per archetype.</summary>
internal unsafe delegate void QueryDelegate(int* componentIds, int n, ChunkCallback cb);

internal delegate int RegisterModComponentDelegate(ModComponentRegistration registration, NativeString256 displayName);