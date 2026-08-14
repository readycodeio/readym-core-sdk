// -------------------------------------------------------------------------
// Delegates and pointers - simplified now that IDs are plain ints
// -------------------------------------------------------------------------

using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using Yooni.Native.Container;

namespace ReadyM.Api.Multiplayer.Interop;

internal delegate int GetComponentIdByNameDelegate(NativeString256 typeName);
internal delegate ArchetypeId RegisterArchetypeDelegate(NativeList<int> componentsSerialized);
internal delegate void ModifyArchetypeDelegate(ArchetypeId archetype, NativeList<int> componentsSerialized);
internal delegate int CreateNetworkedEntityDelegate(ArchetypeId archetype);

/// <summary>Creates a server-only entity: no metadata, never replicated to clients.</summary>
internal delegate int CreateLocalEntityDelegate(ArchetypeId archetype);

/// <summary>1 if the entity existed and was deleted, else 0.</summary>
internal delegate int DeleteNetworkedEntityDelegate(int entityId);

/// <summary>Deletes an entity together with every entity below it in the tree.</summary>
internal delegate int DeleteEntityTreeDelegate(int entityId);

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
internal delegate IntPtr GetComponentPointerDelegate(int entityId, int componentType);
internal unsafe delegate int WriteSnapshotDelegate(IntPtr componentPtr, byte* buffer, int bufferSize);
internal unsafe delegate int WriteDeltaDelegate(IntPtr componentPtr, byte* buffer, int bufferSize);
internal unsafe delegate int ReadSnapshotDelegate(IntPtr componentPtr, byte* buffer, int size);
internal unsafe delegate int ReadDeltaDelegate(IntPtr componentPtr, byte* buffer, int size, byte clearDirty);

/// <summary>1 if the component was changed from the API (a server override), else 0.</summary>
internal delegate byte ChangedFromApiDelegate(IntPtr componentPtr);


// Mod query chunk callbacks: (data ptr, entity count, stride per element).
// Same format for both AOT and mod components on the mod (CoreCLR) side.
internal delegate void ChunkCallback1(IntPtr d1, int count, int s1);
internal delegate void ChunkCallback2(IntPtr d1, IntPtr d2, int count, int s1, int s2);
internal delegate void ChunkCallback3(IntPtr d1, IntPtr d2, IntPtr d3, int count, int s1, int s2, int s3);
internal delegate void ChunkCallback4(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, int count, int s1, int s2, int s3, int s4);
internal delegate void ChunkCallback5(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, int count, int s1, int s2, int s3, int s4, int s5);
internal delegate void ChunkCallback6(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, IntPtr d6, int count, int s1, int s2, int s3, int s4, int s5, int s6);

// Same, but the chunk also carries the entity id of each element, so a system can address the row
// it is looking at without the component having to store its own id.
internal delegate void ChunkWithIdsCallback1(IntPtr ids, IntPtr d1, int count, int s1);
internal delegate void ChunkWithIdsCallback2(IntPtr ids, IntPtr d1, IntPtr d2, int count, int s1, int s2);

internal delegate int  RegisterModComponentDelegate(ModComponentRegistration registration, NativeString256 displayName);
internal delegate void Query1WithIdsDelegate(int c1, ChunkWithIdsCallback1 cb);
internal delegate void Query2WithIdsDelegate(int c1, int c2, ChunkWithIdsCallback2 cb);
internal delegate void Query1Delegate(int c1, ChunkCallback1 cb);
internal delegate void Query2Delegate(int c1, int c2, ChunkCallback2 cb);
internal delegate void Query3Delegate(int c1, int c2, int c3, ChunkCallback3 cb);
internal delegate void Query4Delegate(int c1, int c2, int c3, int c4, ChunkCallback4 cb);
internal delegate void Query5Delegate(int c1, int c2, int c3, int c4, int c5, ChunkCallback5 cb);
internal delegate void Query6Delegate(int c1, int c2, int c3, int c4, int c5, int c6, ChunkCallback6 cb);