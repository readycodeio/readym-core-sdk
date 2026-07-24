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

// Server-side function pointer types
internal delegate int  RegisterModComponentDelegate(ModComponentRegistration registration);
internal delegate void Query1Delegate(int c1, ChunkCallback1 cb);
internal delegate void Query2Delegate(int c1, int c2, ChunkCallback2 cb);
internal delegate void Query3Delegate(int c1, int c2, int c3, ChunkCallback3 cb);
internal delegate void Query4Delegate(int c1, int c2, int c3, int c4, ChunkCallback4 cb);
internal delegate void Query5Delegate(int c1, int c2, int c3, int c4, int c5, ChunkCallback5 cb);
internal delegate void Query6Delegate(int c1, int c2, int c3, int c4, int c5, int c6, ChunkCallback6 cb);