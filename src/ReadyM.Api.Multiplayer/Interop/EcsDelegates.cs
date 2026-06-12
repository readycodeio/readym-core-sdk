// -------------------------------------------------------------------------
// Delegates and pointers - simplified now that IDs are plain ints
// -------------------------------------------------------------------------

using System;
using Friflo.Engine.ECS;
using ReadyM.Api.Idents;
using Yooni.Native.Container;

namespace ReadyM.Api.Multiplayer.Interop;

public delegate int GetComponentIdByNameDelegate(NativeString256 typeName);
public delegate ArchetypeId RegisterArchetypeDelegate(NativeList<int> componentsSerialized);
public delegate void ModifyArchetypeDelegate(ArchetypeId archetype, NativeList<int> componentsSerialized);
public delegate int CreateNetworkedEntityDelegate(ArchetypeId archetype);
public delegate IntPtr GetComponentPointerDelegate(int entityId, int componentType);
public unsafe delegate int WriteSnapshotDelegate(IntPtr componentPtr, byte* buffer, int bufferSize);
public unsafe delegate int WriteDeltaDelegate(IntPtr componentPtr, byte* buffer, int bufferSize);
public unsafe delegate int ReadSnapshotDelegate(IntPtr componentPtr, byte* buffer, int size);
public unsafe delegate int ReadDeltaDelegate(IntPtr componentPtr, byte* buffer, int size, byte clearDirty);


// Plugin query chunk callbacks: (data ptr, entity count, stride per element).
// Same format for both AOT and plugin components on the plugin (CoreCLR) side.
public delegate void ChunkCallback1(IntPtr d1, int count, int s1);
public delegate void ChunkCallback2(IntPtr d1, IntPtr d2, int count, int s1, int s2);
public delegate void ChunkCallback3(IntPtr d1, IntPtr d2, IntPtr d3, int count, int s1, int s2, int s3);
public delegate void ChunkCallback4(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, int count, int s1, int s2, int s3, int s4);
public delegate void ChunkCallback5(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, int count, int s1, int s2, int s3, int s4, int s5);
public delegate void ChunkCallback6(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, IntPtr d6, int count, int s1, int s2, int s3, int s4, int s5, int s6);

// Server-side function pointer types
public delegate int  RegisterPluginComponentDelegate(PluginComponentRegistration registration);
public delegate void Query1Delegate(int c1, ChunkCallback1 cb);
public delegate void Query2Delegate(int c1, int c2, ChunkCallback2 cb);
public delegate void Query3Delegate(int c1, int c2, int c3, ChunkCallback3 cb);
public delegate void Query4Delegate(int c1, int c2, int c3, int c4, ChunkCallback4 cb);
public delegate void Query5Delegate(int c1, int c2, int c3, int c4, int c5, ChunkCallback5 cb);
public delegate void Query6Delegate(int c1, int c2, int c3, int c4, int c5, int c6, ChunkCallback6 cb);