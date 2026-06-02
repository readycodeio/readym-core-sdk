// -------------------------------------------------------------------------
// Delegates and pointers - simplified now that IDs are plain ints
// -------------------------------------------------------------------------

using System;
using ReadyM.Api.Multiplayer.ECS.Registry;
using Yooni.Native.Container;

public delegate int GetComponentIdByNameDelegate(NativeString256 typeName);

// Plugin query chunk callbacks: (data ptr, entity count, stride per element).
// Same format for both AOT and plugin components on the plugin (CoreCLR) side.
public delegate void ChunkCallback1(IntPtr d1, int count, int s1);
public delegate void ChunkCallback2(IntPtr d1, IntPtr d2, int count, int s1, int s2);
public delegate void ChunkCallback3(IntPtr d1, IntPtr d2, IntPtr d3, int count, int s1, int s2, int s3);
public delegate void ChunkCallback4(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, int count, int s1, int s2, int s3, int s4);
public delegate void ChunkCallback5(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, int count, int s1, int s2, int s3, int s4, int s5);
public delegate void ChunkCallback6(IntPtr d1, IntPtr d2, IntPtr d3, IntPtr d4, IntPtr d5, IntPtr d6, int count, int s1, int s2, int s3, int s4, int s5, int s6);

// Server-side function pointer types
public delegate int  RegisterPluginComponentDelegate(int stride);
public delegate void Query1Delegate(int c1, ChunkCallback1 cb);
public delegate void Query2Delegate(int c1, int c2, ChunkCallback2 cb);
public delegate void Query3Delegate(int c1, int c2, int c3, ChunkCallback3 cb);
public delegate void Query4Delegate(int c1, int c2, int c3, int c4, ChunkCallback4 cb);
public delegate void Query5Delegate(int c1, int c2, int c3, int c4, int c5, ChunkCallback5 cb);
public delegate void Query6Delegate(int c1, int c2, int c3, int c4, int c5, int c6, ChunkCallback6 cb);

// Plugin-side query callbacks - the typed ref-based API the plugin author writes against
public delegate void EmbedForEach<T1>(ref T1 c1)
    where T1 : struct;

public delegate void EmbedForEach<T1, T2>(ref T1 c1, ref T2 c2)
    where T1 : struct where T2 : struct;

public delegate void EmbedForEach<T1, T2, T3>(ref T1 c1, ref T2 c2, ref T3 c3)
    where T1 : struct where T2 : struct where T3 : struct;

public delegate void EmbedForEach<T1, T2, T3, T4>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4)
    where T1 : struct where T2 : struct where T3 : struct where T4 : struct;

public delegate void EmbedForEach<T1, T2, T3, T4, T5>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5)
    where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct;

public delegate void EmbedForEach<T1, T2, T3, T4, T5, T6>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6)
    where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct;